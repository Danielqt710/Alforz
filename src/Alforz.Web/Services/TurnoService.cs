using System.Net;
using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Services;

public class TurnoService(
    ITurnoRepository turnos,
    IUsuarioRepository usuarios,
    IRobotRepository robots,
    IEmailService correo,
    IOptions<TurnosSettings> opciones,
    ILogger<TurnoService> logger) : ITurnoService
{
    private const int LargoMaxComentario = 300;

    private readonly TurnosSettings _config = opciones.Value;

    private readonly TimeZoneInfo _zona = BuscarZona(opciones.Value.ZonaHoraria);

    public int DuracionHoras => _config.DuracionHoras;

    /// <summary>Fecha y hora actuales en la zona del negocio.</summary>
    private DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zona);

    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    public DateOnly UltimoDiaAgendable => Hoy.AddDays(_config.DiasAnticipacionMax);

    public bool EsDiaLaborable(DateOnly dia) => _config.DiasLaborables.Contains((int)dia.DayOfWeek);

    public DateOnly PrimerDiaDisponible()
    {
        for (var dia = Hoy; dia <= UltimoDiaAgendable; dia = dia.AddDays(1))
        {
            if (EsDiaLaborable(dia) && HorasDelDia().Any(h => !YaEmpezo(dia, h)))
                return dia;
        }
        return Hoy;
    }

    public async Task<IReadOnlyList<Horario>> HorariosDelDiaAsync(DateOnly dia)
    {
        if (!EsDiaLaborable(dia) || dia < Hoy || dia > UltimoDiaAgendable)
            return [];

        var ocupadas = (await turnos.ListarActivosDelDiaAsync(FormatoTurno.Fecha(dia)))
            .Select(t => t.HoraInicio)
            .ToHashSet();

        return HorasDelDia()
            .Select(hora => new Horario(hora, _config.DuracionHoras,
                ocupadas.Contains(hora) ? EstadoHorario.Ocupado
                : YaEmpezo(dia, hora) ? EstadoHorario.Pasado
                : EstadoHorario.Libre))
            .ToList();
    }

    public async Task<ResultadoTurno> ReservarAsync(
        string usuarioId, string nombreUsuario, string correoUsuario, DateOnly dia, int horaInicio, string robotId, string? comentario)
    {
        if (!EsDiaLaborable(dia) || dia < Hoy || dia > UltimoDiaAgendable || !HorasDelDia().Contains(horaInicio))
            return ResultadoTurno.Falla("Ese horario no está disponible para agendar.");

        if (YaEmpezo(dia, horaInicio))
            return ResultadoTurno.Falla("Ese horario ya pasó. Elige otro.");

        var robot = await robots.ObtenerPorIdAsync(robotId);
        if (robot is not { Activo: true })
            return ResultadoTurno.Falla("Elige qué robot quieres ver.");

        var activos = (await turnos.ListarDeUsuarioAsync(usuarioId)).Count(t => t.EstaActivo && !YaPaso(t));
        if (activos >= _config.MaxTurnosActivosPorUsuario)
            return ResultadoTurno.Falla(
                $"Ya tienes {activos} turnos agendados, que es el máximo. Espera a que pase alguno para agendar otro.");

        var nota = (comentario ?? string.Empty).Trim();
        if (nota.Length > LargoMaxComentario)
            nota = nota[..LargoMaxComentario];

        var turno = new Turno
        {
            UsuarioId = usuarioId,
            NombreUsuario = nombreUsuario,
            Correo = correoUsuario,
            Fecha = FormatoTurno.Fecha(dia),
            HoraInicio = horaInicio,
            DuracionHoras = _config.DuracionHoras,
            Comentario = nota,
            RobotId = robot.Id,
            RobotNombre = robot.Nombre,
            RobotPrecio = robot.Precio,
            FechaCreacion = DateTime.UtcNow
        };

        if (!await turnos.CrearSiEstaLibreAsync(turno))
            return ResultadoTurno.Falla("Alguien acaba de agendar ese horario. Elige otro.");

        logger.LogInformation("Turno {TurnoId} agendado por {UsuarioId} para {Fecha} {Hora}:00",
            turno.Id, usuarioId, turno.Fecha, turno.HoraInicio);

        var enviado = await IntentarEnviarAsync(turno, "Tu turno en Alforz está confirmado", "Turno confirmado",
            "Tu turno quedó agendado. Te esperamos:");
        return new ResultadoTurno(true, Turno: turno, CorreoEnviado: enviado);
    }

    public async Task<IReadOnlyList<Turno>> TurnosDeUsuarioAsync(string usuarioId) =>
        (await turnos.ListarDeUsuarioAsync(usuarioId))
            .Where(t => t.EstaActivo && !YaPaso(t))
            .OrderBy(t => t.Inicio)
            .ToList();

    public async Task<ProgresoPremio> ProgresoPremioAsync(string usuarioId)
    {
        var asistidos = (await turnos.ListarDeUsuarioAsync(usuarioId)).Count(CuentaParaPremio);
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId);
        return new ProgresoPremio(asistidos, MetaPremio, usuario?.PremiosEntregados ?? 0);
    }

    public async Task<IReadOnlyList<PremioUsuario>> ListarPremiosAsync()
    {
        var asistidosPorUsuario = (await turnos.ListarAsync())
            .Where(CuentaParaPremio)
            .GroupBy(t => t.UsuarioId)
            .ToDictionary(g => g.Key, g => g.Count());

        return (await usuarios.ListarAsync())
            .Where(u => asistidosPorUsuario.ContainsKey(u.Id))
            .Select(u => new PremioUsuario(u, new ProgresoPremio(asistidosPorUsuario[u.Id], MetaPremio, u.PremiosEntregados)))
            .OrderByDescending(p => p.Progreso.PorRetirar)
            .ThenByDescending(p => p.Progreso.Asistidos)
            .ToList();
    }

    public async Task<Resultado> EntregarPremioAsync(string usuarioId, string entregadoPor)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId);
        if (usuario is null)
            return Resultado.Falla("Ese usuario no existe.");

        var progreso = await ProgresoPremioAsync(usuarioId);
        if (progreso.PorRetirar == 0)
            return Resultado.Falla($"{usuario.NombreUsuario} no tiene premios por retirar.");

        usuario.PremiosEntregados++;
        await usuarios.ActualizarAsync(usuario);
        logger.LogInformation("Premio entregado a {UsuarioId} por {Admin} ({Entregados} en total)",
            usuarioId, entregadoPor, usuario.PremiosEntregados);
        return Resultado.Ok;
    }

    public async Task<IReadOnlyList<Turno>> ListarAsync(FiltroTurnos filtro)
    {
        var todos = await turnos.ListarAsync();
        return filtro switch
        {
            FiltroTurnos.Proximos => todos.Where(t => t.EstaActivo && !YaPaso(t)).OrderBy(t => t.Inicio).ToList(),
            FiltroTurnos.Pasados => todos.Where(t => t.EstaActivo && YaPaso(t)).OrderByDescending(t => t.Inicio).ToList(),
            FiltroTurnos.Cancelados => todos.Where(t => !t.EstaActivo).OrderByDescending(t => t.Inicio).ToList(),
            _ => todos.OrderByDescending(t => t.Inicio).ToList()
        };
    }

    public async Task<ResultadoTurno> CancelarAsync(string turnoId, string canceladoPor)
    {
        var turno = await turnos.ObtenerPorIdAsync(turnoId);
        if (turno is null)
            return ResultadoTurno.Falla("Ese turno no existe.");

        if (!turno.EstaActivo)
            return ResultadoTurno.Falla("Ese turno ya estaba cancelado.");

        turno.Estado = EstadoTurno.Cancelado;
        turno.FechaCancelacion = DateTime.UtcNow;
        turno.CanceladoPor = canceladoPor;
        await turnos.ActualizarAsync(turno);
        logger.LogInformation("Turno {TurnoId} cancelado por {Admin}", turno.Id, canceladoPor);

        // Si el turno ya pasó no tiene sentido avisar.
        var enviado = !YaPaso(turno) && await IntentarEnviarAsync(turno, "Tu turno en Alforz fue cancelado",
            "Turno cancelado", "Lamentamos avisarte que este turno fue cancelado. Puedes agendar otro cuando quieras:");
        return new ResultadoTurno(true, Turno: turno, CorreoEnviado: enviado);
    }

    public async Task<ResultadoTurno> MarcarAsistenciaAsync(string turnoId, bool asistio, string marcadoPor)
    {
        var turno = await turnos.ObtenerPorIdAsync(turnoId);
        if (turno is null)
            return ResultadoTurno.Falla("Ese turno no existe.");

        if (!turno.EstaActivo)
            return ResultadoTurno.Falla("No se puede marcar asistencia en un turno cancelado.");

        if (!YaPaso(turno))
            return ResultadoTurno.Falla("La asistencia se marca cuando el turno ya empezó.");

        turno.Asistio = asistio;
        turno.AsistenciaMarcadaPor = marcadoPor;
        turno.FechaAsistencia = DateTime.UtcNow;
        await turnos.ActualizarAsync(turno);
        logger.LogInformation("Turno {TurnoId}: asistencia {Asistio} marcada por {Admin}", turno.Id, asistio, marcadoPor);

        var (completado, enviado) = asistio ? await FelicitarSiCompletoAsync(turno.UsuarioId) : (false, false);
        return new ResultadoTurno(true, Turno: turno, CorreoEnviado: enviado, PremioCompletado: completado);
    }

    /// <summary>
    /// Si el usuario tiene más premios ganados que avisados, le manda el correo de felicitación una sola vez
    /// (aunque el Admin desmarque y vuelva a marcar la asistencia).
    /// </summary>
    private async Task<(bool Completado, bool Enviado)> FelicitarSiCompletoAsync(string usuarioId)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId);
        if (usuario is null)
            return (false, false);

        var progreso = await ProgresoPremioAsync(usuarioId);
        if (progreso.PremiosGanados <= usuario.PremiosNotificados)
            return (false, false);

        usuario.PremiosNotificados = progreso.PremiosGanados;
        await usuarios.ActualizarAsync(usuario);
        logger.LogInformation("{UsuarioId} completó el camino de premios ({Premios} en total)", usuarioId, progreso.PremiosGanados);

        try
        {
            await correo.EnviarAsync(usuario.Correo, "¡Ganaste un premio sorpresa en Alforz! 🎁", ArmarCorreoPremio(usuario.NombreUsuario));
            return (true, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar el correo de premio a {UsuarioId}", usuarioId);
            return (true, false);
        }
    }

    private int MetaPremio => Math.Max(1, _config.AsistenciasParaPremio);

    private static bool CuentaParaPremio(Turno turno) => turno.EstaActivo && turno.Asistio == true;

    public bool YaPaso(Turno turno) => turno.Inicio <= Ahora;

    private IEnumerable<int> HorasDelDia()
    {
        for (var hora = _config.HoraApertura; hora + _config.DuracionHoras <= _config.HoraCierre; hora += _config.DuracionHoras)
            yield return hora;
    }

    /// <summary>Un horario ya no se puede agendar desde que empieza.</summary>
    private bool YaEmpezo(DateOnly dia, int hora) => dia.ToDateTime(new TimeOnly(hora, 0)) <= Ahora;

    private async Task<bool> IntentarEnviarAsync(Turno turno, string asunto, string titulo, string texto)
    {
        try
        {
            await correo.EnviarAsync(turno.Correo, asunto, ArmarCorreo(turno, titulo, texto));
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar el correo del turno {TurnoId}", turno.Id);
            return false;
        }
    }

    private static string ArmarCorreo(Turno turno, string titulo, string texto)
    {
        var comentario = string.IsNullOrWhiteSpace(turno.Comentario)
            ? string.Empty
            : $"<p><strong>Comentario:</strong> {WebUtility.HtmlEncode(turno.Comentario)}</p>";
        var robot = turno.TieneRobot
            ? $"<p><strong>Robot:</strong> {WebUtility.HtmlEncode(turno.RobotNombre)} · {FormatoPrecio.Dolares(turno.RobotPrecio)}</p>"
            : string.Empty;

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;color:#2F3A4A;max-width:480px;margin:auto">
              <h2 style="background:#BFE3F2;padding:16px;border-radius:12px;margin:0 0 16px">{titulo}</h2>
              <p>Hola {WebUtility.HtmlEncode(turno.NombreUsuario)},</p>
              <p>{texto}</p>
              <div style="background:#D6D1F5;padding:16px;border-radius:12px;text-align:center">
                <div style="font-size:18px;font-weight:bold">{FormatoTurno.Dia(turno.Dia)}</div>
                <div style="font-size:24px;font-weight:bold;margin-top:4px">{FormatoTurno.Horario(turno.HoraInicio, turno.DuracionHoras)}</div>
              </div>
              {robot}
              {comentario}
              <p style="margin-top:16px">Alforz Robotics</p>
            </div>
            """;
    }

    private string ArmarCorreoPremio(string nombre) => $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;color:#2F3A4A;max-width:480px;margin:auto">
          <h2 style="background:#FFE7A3;padding:16px;border-radius:12px;margin:0 0 16px">¡Completaste tu camino de premios! 🎉</h2>
          <p>Hola {WebUtility.HtmlEncode(nombre)},</p>
          <p>Asististe a {MetaPremio} turnos con nosotros y llenaste todos los círculos de tu camino.</p>
          <p style="font-size:40px;text-align:center;margin:8px 0">🎁</p>
          <p style="background:#C8EBDD;padding:16px;border-radius:12px;text-align:center;font-weight:bold">
            Ganaste un premio sorpresa. Te lo entregamos en tu próximo turno.
          </p>
          <p>¡Gracias por elegirnos! Con tu próximo turno empieza un camino nuevo hacia otro premio.</p>
          <p style="margin-top:16px">Alforz Robotics</p>
        </div>
        """;

    private static TimeZoneInfo BuscarZona(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Sin datos de zonas en el sistema: Argentina no tiene horario de verano, UTC-3 fijo alcanza.
            return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(-3), id, id);
        }
    }
}
