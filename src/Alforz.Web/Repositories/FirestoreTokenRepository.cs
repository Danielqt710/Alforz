using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Google.Cloud.Firestore;

namespace Alforz.Web.Repositories;

/// <summary>
/// Las consultas solo usan filtros de igualdad, así Firestore no pide índices compuestos.
/// El orden y el vencimiento se resuelven en memoria (son pocos tokens por usuario).
/// </summary>
public class FirestoreTokenRepository(FirestoreProveedor firestore) : ITokenRepository
{
    public const string Coleccion = "tokens";

    private CollectionReference Tokens => firestore.Db.Collection(Coleccion);

    public async Task<string> CrearAsync(TokenSeguridad token)
    {
        var referencia = await Tokens.AddAsync(token);
        token.Id = referencia.Id;
        return referencia.Id;
    }

    public async Task<TokenSeguridad?> ObtenerVigenteAsync(string usuarioId, TipoToken tipo)
    {
        var ahora = DateTime.UtcNow;
        return (await NoUsadosAsync(usuarioId, tipo))
            .Where(t => t.EsValido(ahora))
            .OrderByDescending(t => t.FechaCreacion)
            .FirstOrDefault();
    }

    public Task ActualizarAsync(TokenSeguridad token) => Tokens.Document(token.Id).SetAsync(token);

    public async Task InvalidarAnterioresAsync(string usuarioId, TipoToken tipo)
    {
        var pendientes = await NoUsadosAsync(usuarioId, tipo);
        if (pendientes.Count == 0)
            return;

        var lote = firestore.Db.StartBatch();
        foreach (var token in pendientes)
            lote.Update(Tokens.Document(token.Id), nameof(TokenSeguridad.Usado), true);
        await lote.CommitAsync();
    }

    private async Task<List<TokenSeguridad>> NoUsadosAsync(string usuarioId, TipoToken tipo)
    {
        var snapshot = await Tokens
            .WhereEqualTo(nameof(TokenSeguridad.UsuarioId), usuarioId)
            .WhereEqualTo(nameof(TokenSeguridad.Tipo), (int)tipo)
            .WhereEqualTo(nameof(TokenSeguridad.Usado), false)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<TokenSeguridad>()).ToList();
    }
}
