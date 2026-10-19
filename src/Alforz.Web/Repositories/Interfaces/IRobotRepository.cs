using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a las colecciones "robots" e "imagenesRobots".</summary>
public interface IRobotRepository
{
    Task<Robot?> ObtenerPorIdAsync(string id);

    /// <summary>Todos los robots, del más barato al más caro.</summary>
    Task<IReadOnlyList<Robot>> ListarAsync();

    Task<bool> HayAlgunoAsync();
    Task<string> CrearAsync(Robot robot);
    Task ActualizarAsync(Robot robot);

    Task<ImagenRobot?> ObtenerImagenAsync(string robotId);
    Task GuardarImagenAsync(string robotId, byte[] datos, string tipoContenido);
    Task BorrarImagenAsync(string robotId);
}
