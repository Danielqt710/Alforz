using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a la colección "usuarios". La implementación concreta usa Firestore.</summary>
public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(string id);
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);
    Task<IReadOnlyList<Usuario>> ListarAsync();
    Task<bool> ExisteAlgunAdminAsync();
    Task<string> CrearAsync(Usuario usuario);
    Task ActualizarAsync(Usuario usuario);
}
