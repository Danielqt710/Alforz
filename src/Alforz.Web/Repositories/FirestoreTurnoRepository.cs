using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Google.Cloud.Firestore;

namespace Alforz.Web.Repositories;

/// <summary>Solo filtros de igualdad: Firestore no pide índices compuestos. El orden se hace en memoria.</summary>
public class FirestoreTurnoRepository(FirestoreProveedor firestore) : ITurnoRepository
{
    public const string Coleccion = "turnos";

    private CollectionReference Turnos => firestore.Db.Collection(Coleccion);

    public async Task<Turno?> ObtenerPorIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var snapshot = await Turnos.Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Turno>() : null;
    }

    public async Task<IReadOnlyList<Turno>> ListarActivosDelDiaAsync(string fecha)
    {
        var snapshot = await ActivosDelHorario(fecha).GetSnapshotAsync();
        return Convertir(snapshot);
    }

    public async Task<IReadOnlyList<Turno>> ListarDeUsuarioAsync(string usuarioId)
    {
        var snapshot = await Turnos.WhereEqualTo(nameof(Turno.UsuarioId), usuarioId).GetSnapshotAsync();
        return Convertir(snapshot);
    }

    public async Task<IReadOnlyList<Turno>> ListarAsync() => Convertir(await Turnos.GetSnapshotAsync());

    public Task<bool> CrearSiEstaLibreAsync(Turno turno) =>
        firestore.Db.RunTransactionAsync(async transaccion =>
        {
            var ocupado = await transaccion.GetSnapshotAsync(
                ActivosDelHorario(turno.Fecha).WhereEqualTo(nameof(Turno.HoraInicio), turno.HoraInicio));
            if (ocupado.Count > 0)
                return false;

            var documento = Turnos.Document();
            turno.Id = documento.Id;
            transaccion.Create(documento, turno);
            return true;
        });

    public Task ActualizarAsync(Turno turno) => Turnos.Document(turno.Id).SetAsync(turno);

    private Query ActivosDelHorario(string fecha) => Turnos
        .WhereEqualTo(nameof(Turno.Fecha), fecha)
        .WhereEqualTo(nameof(Turno.Estado), EstadoTurno.Activo);

    private static List<Turno> Convertir(QuerySnapshot snapshot) =>
        snapshot.Documents.Select(d => d.ConvertTo<Turno>()).ToList();
}
