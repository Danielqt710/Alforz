using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Google.Cloud.Firestore;

namespace Alforz.Web.Repositories;

public class FirestoreUsuarioRepository(FirestoreProveedor firestore) : IUsuarioRepository
{
    public const string Coleccion = "usuarios";

    private CollectionReference Usuarios => firestore.Db.Collection(Coleccion);

    public async Task<Usuario?> ObtenerPorIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var snapshot = await Usuarios.Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Usuario>() : null;
    }

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario) =>
        PrimeroDondeAsync(nameof(Usuario.NombreUsuarioNormalizado), Usuario.Normalizar(nombreUsuario));

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo) =>
        PrimeroDondeAsync(nameof(Usuario.CorreoNormalizado), Usuario.Normalizar(correo));

    public async Task<IReadOnlyList<Usuario>> ListarAsync()
    {
        var snapshot = await Usuarios.OrderBy(nameof(Usuario.NombreUsuarioNormalizado)).GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Usuario>()).ToList();
    }

    public async Task<bool> ExisteAlgunAdminAsync()
    {
        var snapshot = await Usuarios.WhereEqualTo(nameof(Usuario.Rol), Roles.Admin).Limit(1).GetSnapshotAsync();
        return snapshot.Count > 0;
    }

    public async Task<string> CrearAsync(Usuario usuario)
    {
        var referencia = await Usuarios.AddAsync(usuario);
        usuario.Id = referencia.Id;
        return referencia.Id;
    }

    public Task ActualizarAsync(Usuario usuario) => Usuarios.Document(usuario.Id).SetAsync(usuario);

    private async Task<Usuario?> PrimeroDondeAsync(string campo, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var snapshot = await Usuarios.WhereEqualTo(campo, valor).Limit(1).GetSnapshotAsync();
        return snapshot.Count > 0 ? snapshot.Documents[0].ConvertTo<Usuario>() : null;
    }
}
