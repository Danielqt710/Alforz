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
        string usuarioId, string nombreUsuario, string correoUsuario, DateOnly dia, int horaInicio, string? comentario)
    {
        if (!EsDiaLaborable(dia) || dia < Hoy || dia > UltimoDiaAgendable || !HorasDelDia().Contains(horaInicio))
            return ResultadoTurno.Falla("Ese horario no está disponible para agendar.");

        if (YaEmpezo(dia, horaInicio))
            return ResultadoTurno.Falla("Ese horario ya pasó. Elige otro.");

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

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;color:#2F3A4A;max-width:480px;margin:auto">
              <h2 style="background:#BFE3F2;padding:16px;border-radius:12px;margin:0 0 16px">{titulo}</h2>
              <p>Hola {WebUtility.HtmlEncode(turno.NombreUsuario)},</p>
              <p>{texto}</p>
              <div style="background:#D6D1F5;padding:16px;border-radius:12px;text-align:center">
                <div style="font-size:18px;font-weight:bold">{FormatoTurno.Dia(turno.Dia)}</div>
                <div style="font-size:24px;font-weight:bold;margin-top:4px">{FormatoTurno.Horario(turno.HoraInicio, turno.DuracionHoras)}</div>
              </div>
              {comentario}
              <p style="margin-top:16px">Alforz Robotics</p>
            </div>
            """;
    }

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
