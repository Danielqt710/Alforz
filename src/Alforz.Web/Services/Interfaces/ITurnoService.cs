using Alforz.Web.Models.Entities;

namespace Alforz.Web.Services.Interfaces;

public enum EstadoHorario
{
    Libre,
    Ocupado,
    Pasado
}

public record Horario(int HoraInicio, int DuracionHoras, EstadoHorario Estado);

/// <summary>
/// Resultado de reservar, cancelar o marcar asistencia. Si falla solo el correo, la operación igual queda hecha.
/// <paramref name="PremioCompletado"/>: al marcar asistencia, el usuario acaba de completar el camino de premios.
/// </summary>
public record ResultadoTurno(
    bool Exito, string? Error = null, Turno? Turno = null, bool CorreoEnviado = false, bool PremioCompletado = false)
{
    public static ResultadoTurno Falla(string error) => new(false, error);
}

/// <summary>
/// Avance en el camino de premios. Al completar la meta se gana un premio y el camino
/// vuelve a empezar; justo al completarlo se muestra lleno hasta el próximo turno asistido.
/// </summary>
public record ProgresoPremio(int Asistidos, int Meta, int Entregados = 0)
{
    public int PremiosGanados => Asistidos / Meta;

    /// <summary>Premios ganados que el Admin todavía no entregó.</summary>
    public int PorRetirar => Math.Max(0, PremiosGanados - Entregados);

    /// <summary>Círculos llenos en el camino actual (0..Meta).</summary>
    public int Llenos => Asistidos > 0 && Asistidos % Meta == 0 ? Meta : Asistidos % Meta;

    public bool Completo => Llenos == Meta;

    public int Faltan => Meta - Llenos;
}

/// <summary>Una fila de la página de premios del Admin.</summary>
public record PremioUsuario(Usuario Usuario, ProgresoPremio Progreso);

public enum FiltroTurnos
{
    Proximos,
    Pasados,
    Cancelados,
    Todos
}

/// <summary>Reglas para agendar y cancelar turnos de 2 horas.</summary>
public interface ITurnoService
{
    DateOnly Hoy { get; }
    DateOnly UltimoDiaAgendable { get; }
    int DuracionHoras { get; }

    bool EsDiaLaborable(DateOnly dia);

    /// <summary>Primer día laborable desde hoy que todavía tiene horarios por delante.</summary>
    DateOnly PrimerDiaDisponible();

    /// <summary>Horarios del día con su estado. Vacío si el día no tiene atención o está fuera de rango.</summary>
    Task<IReadOnlyList<Horario>> HorariosDelDiaAsync(DateOnly dia);

    Task<ResultadoTurno> ReservarAsync(
        string usuarioId, string nombreUsuario, string correo, DateOnly dia, int horaInicio, string robotId, string? comentario);

    Task<IReadOnlyList<Turno>> TurnosDeUsuarioAsync(string usuarioId);

    /// <summary>Cuántos turnos asistió el usuario y cómo va en el camino de premios.</summary>
    Task<ProgresoPremio> ProgresoPremioAsync(string usuarioId);

    Task<IReadOnlyList<Turno>> ListarAsync(FiltroTurnos filtro);

    /// <summary>Cancela un turno activo y avisa por correo a quien lo había agendado.</summary>
    Task<ResultadoTurno> CancelarAsync(string turnoId, string canceladoPor);

    /// <summary>Marca si la persona asistió. Solo para turnos activos que ya empezaron; se puede corregir.</summary>
    Task<ResultadoTurno> MarcarAsistenciaAsync(string turnoId, bool asistio, string marcadoPor);

    /// <summary>Usuarios con al menos un turno asistido: primero los que tienen premios por retirar.</summary>
    Task<IReadOnlyList<PremioUsuario>> ListarPremiosAsync();

    /// <summary>Registra que el Admin entregó un premio (solo si el usuario tiene alguno por retirar).</summary>
    Task<Resultado> EntregarPremioAsync(string usuarioId, string entregadoPor);

    /// <summary>Si el turno ya empezó o terminó (según la hora del negocio).</summary>
    bool YaPaso(Turno turno);
}
