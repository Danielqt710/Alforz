using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Google.Cloud.Firestore;

namespace Alforz.Web.Repositories;

public class FirestoreMovimientoStockRepository(FirestoreProveedor firestore) : IMovimientoStockRepository
{
    public const string Coleccion = "movimientosStock";

    private CollectionReference Movimientos => firestore.Db.Collection(Coleccion);

    public Task RegistrarAsync(MovimientoStock movimiento) => Movimientos.AddAsync(movimiento);

    public async Task<IReadOnlyList<MovimientoStock>> ListarPorRobotAsync(string robotId)
    {
        if (string.IsNullOrWhiteSpace(robotId))
            return [];

        // Solo se filtra por robot (índice simple); ordenar por fecha en Firestore pediría un índice compuesto.
        var snapshot = await Movimientos.WhereEqualTo(nameof(MovimientoStock.RobotId), robotId).GetSnapshotAsync();
        return snapshot.Documents
            .Select(d => d.ConvertTo<MovimientoStock>())
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }
}
