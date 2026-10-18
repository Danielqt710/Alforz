using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a la colección "turnos".</summary>
public interface ITurnoRepository
{
    Task<Turno?> ObtenerPorIdAsync(string id);
    Task<IReadOnlyList<Turno>> ListarActivosDelDiaAsync(string fecha);
    Task<IReadOnlyList<Turno>> ListarDeUsuarioAsync(string usuarioId);
    Task<IReadOnlyList<Turno>> ListarAsync();

    /// <summary>
    /// Crea el turno solo si ese horario sigue libre (en una transacción, así dos personas
    /// no pueden quedarse con el mismo horario). Devuelve false si ya estaba ocupado.
    /// </summary>
    Task<bool> CrearSiEstaLibreAsync(Turno turno);

    Task ActualizarAsync(Turno turno);
}
