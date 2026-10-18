using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

public enum TipoToken
{
    VerificacionCorreo,
    RecuperacionClave
}

/// <summary>
/// Código de un solo uso guardado en la colección "tokens".
/// Solo se guarda el hash del código; el código real viaja únicamente por correo.
/// </summary>
[FirestoreData]
public class TokenSeguridad
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UsuarioId { get; set; } = string.Empty;

    /// <summary>Se guarda como número (0, 1).</summary>
    [FirestoreProperty]
    public TipoToken Tipo { get; set; }

    [FirestoreProperty]
    public string CodigoHash { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime ExpiraEn { get; set; }

    [FirestoreProperty]
    public int Intentos { get; set; }

    [FirestoreProperty]
    public bool Usado { get; set; }

    [FirestoreProperty]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool EsValido(DateTime ahoraUtc) => !Usado && ExpiraEn > ahoraUtc;
}
