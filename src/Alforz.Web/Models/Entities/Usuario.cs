namespace Alforz.Web.Models.Entities;

/// <summary>
/// Usuario guardado en la colección "usuarios" de Firestore.
/// Cuando se agregue el paquete Google.Cloud.Firestore, esta clase lleva
/// [FirestoreData] y cada propiedad [FirestoreProperty].
/// </summary>
public class Usuario
{
    /// <summary>Id del documento en Firestore.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre con el que se loguea, tal como lo escribió.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Nombre en minúsculas, para buscar sin importar mayúsculas.</summary>
    public string NombreUsuarioNormalizado { get; set; } = string.Empty;

    /// <summary>Correo para seguridad: verificación, tokens y recuperación de clave.</summary>
    public string Correo { get; set; } = string.Empty;

    public string CorreoNormalizado { get; set; } = string.Empty;

    public bool CorreoVerificado { get; set; }

    /// <summary>Hash de la clave (nunca la clave en texto plano).</summary>
    public string ClaveHash { get; set; } = string.Empty;

    /// <summary>Uno de los valores de <see cref="Roles"/>.</summary>
    public string Rol { get; set; } = Roles.Visita;

    public bool Activo { get; set; } = true;

    /// <summary>Intentos de login fallidos seguidos.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Si tiene valor y es futuro, la cuenta está bloqueada hasta esa fecha (UTC).</summary>
    public DateTime? BloqueadoHasta { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoIngreso { get; set; }

    public bool EstaBloqueado(DateTime ahoraUtc) => BloqueadoHasta is { } hasta && hasta > ahoraUtc;
}
