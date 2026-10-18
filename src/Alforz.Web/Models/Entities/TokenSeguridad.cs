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
public class TokenSeguridad
{
    public string Id { get; set; } = string.Empty;

    public string UsuarioId { get; set; } = string.Empty;

    public TipoToken Tipo { get; set; }

    public string CodigoHash { get; set; } = string.Empty;

    public DateTime ExpiraEn { get; set; }

    public int Intentos { get; set; }

    public bool Usado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool EsValido(DateTime ahoraUtc) => !Usado && ExpiraEn > ahoraUtc;
}
