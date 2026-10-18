using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Google.Cloud.Firestore;

namespace Alforz.Web.Repositories;

public class FirestoreRobotRepository(FirestoreProveedor firestore) : IRobotRepository
{
    public const string Coleccion = "robots";

    private CollectionReference Robots => firestore.Db.Collection(Coleccion);

    public async Task<Robot?> ObtenerPorIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var snapshot = await Robots.Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Robot>() : null;
    }

    public async Task<IReadOnlyList<Robot>> ListarAsync()
    {
        var snapshot = await Robots.GetSnapshotAsync();
        return snapshot.Documents
            .Select(d => d.ConvertTo<Robot>())
            .OrderBy(r => r.Precio)
            .ThenBy(r => r.Nombre)
            .ToList();
    }

    public async Task<bool> HayAlgunoAsync() => (await Robots.Limit(1).GetSnapshotAsync()).Count > 0;

    public async Task<string> CrearAsync(Robot robot)
    {
        var referencia = await Robots.AddAsync(robot);
        robot.Id = referencia.Id;
        return referencia.Id;
    }

    public Task ActualizarAsync(Robot robot) => Robots.Document(robot.Id).SetAsync(robot);
}
