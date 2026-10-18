using Alforz.Web.Models.Entities;

namespace Alforz.Web.Services.Interfaces;

public enum EstadoHorario
{
    Libre,
    Ocupado,
    Pasado
}

public record Horario(int HoraInicio, int DuracionHoras, EstadoHorario Estado);

/// <summary>Resultado de reservar o cancelar. Si falla solo el correo, la operación igual queda hecha.</summary>
public record ResultadoTurno(bool Exito, string? Error = null, Turno? Turno = null, bool CorreoEnviado = false)
{
    public static ResultadoTurno Falla(string error) => new(false, error);
}

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
        string usuarioId, string nombreUsuario, string correo, DateOnly dia, int horaInicio, string? comentario);

    Task<IReadOnlyList<Turno>> TurnosDeUsuarioAsync(string usuarioId);

    Task<IReadOnlyList<Turno>> ListarAsync(FiltroTurnos filtro);

    /// <summary>Cancela un turno activo y avisa por correo a quien lo había agendado.</summary>
    Task<ResultadoTurno> CancelarAsync(string turnoId, string canceladoPor);

    /// <summary>Marca si la persona asistió. Solo para turnos activos que ya empezaron; se puede corregir.</summary>
    Task<ResultadoTurno> MarcarAsistenciaAsync(string turnoId, bool asistio, string marcadoPor);

    /// <summary>Si el turno ya empezó o terminó (según la hora del negocio).</summary>
    bool YaPaso(Turno turno);
}
