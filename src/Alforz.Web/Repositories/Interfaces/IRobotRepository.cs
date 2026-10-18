using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a la colección "robots".</summary>
public interface IRobotRepository
{
    Task<Robot?> ObtenerPorIdAsync(string id);

    /// <summary>Todos los robots, del más barato al más caro.</summary>
    Task<IReadOnlyList<Robot>> ListarAsync();

    Task<bool> HayAlgunoAsync();
    Task<string> CrearAsync(Robot robot);
    Task ActualizarAsync(Robot robot);
}
