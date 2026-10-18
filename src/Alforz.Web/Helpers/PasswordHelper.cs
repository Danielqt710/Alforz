using Alforz.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Alforz.Web.Helpers;

/// <summary>
/// Hash y verificación de claves con el PasswordHasher de ASP.NET Core
/// (PBKDF2 con sal aleatoria por clave). Viene en el framework, sin paquetes extra.
/// </summary>
public class PasswordHelper
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(Usuario usuario, string clave) => _hasher.HashPassword(usuario, clave);

    /// <summary>
    /// Devuelve si la clave es correcta y si conviene re-hashearla
    /// (pasa cuando el framework sube la cantidad de iteraciones).
    /// </summary>
    public (bool Correcta, bool RequiereRehash) Verificar(Usuario usuario, string clave)
    {
        if (string.IsNullOrEmpty(usuario.ClaveHash))
            return (false, false);

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.ClaveHash, clave);
        return resultado switch
        {
            PasswordVerificationResult.Success => (true, false),
            PasswordVerificationResult.SuccessRehashNeeded => (true, true),
            _ => (false, false)
        };
    }
}
