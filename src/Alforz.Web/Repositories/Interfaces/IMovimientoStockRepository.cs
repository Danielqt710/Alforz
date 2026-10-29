using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a la colección "movimientosStock" (historial de cantidad y precio de los robots).</summary>
public interface IMovimientoStockRepository
{
    Task RegistrarAsync(MovimientoStock movimiento);

    /// <summary>Movimientos de un robot, del más nuevo al más viejo.</summary>
    Task<IReadOnlyList<MovimientoStock>> ListarPorRobotAsync(string robotId);
}
