using Alforz.Web.Models.Entities;

namespace Alforz.Web.Repositories.Interfaces;

/// <summary>Acceso a la colección "tokens".</summary>
public interface ITokenRepository
{
    Task<string> CrearAsync(TokenSeguridad token);
    Task<TokenSeguridad?> ObtenerVigenteAsync(string usuarioId, TipoToken tipo);
    Task ActualizarAsync(TokenSeguridad token);

    /// <summary>Marca como usados los tokens anteriores del mismo tipo, para que solo valga el último.</summary>
    Task InvalidarAnterioresAsync(string usuarioId, TipoToken tipo);
}
