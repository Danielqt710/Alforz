using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>Usuario guardado en la colección "usuarios" de Firestore.</summary>
[FirestoreData]
public class Usuario
{
    /// <summary>Id del documento en Firestore.</summary>
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre con el que se loguea, tal como lo escribió.</summary>
    [FirestoreProperty]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Nombre en minúsculas, para buscar sin importar mayúsculas.</summary>
    [FirestoreProperty]
    public string NombreUsuarioNormalizado { get; set; } = string.Empty;

    /// <summary>Correo para seguridad: verificación, tokens y recuperación de clave.</summary>
    [FirestoreProperty]
    public string Correo { get; set; } = string.Empty;

    [FirestoreProperty]
    public string CorreoNormalizado { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool CorreoVerificado { get; set; }

    /// <summary>Hash de la clave (nunca la clave en texto plano).</summary>
    [FirestoreProperty]
    public string ClaveHash { get; set; } = string.Empty;

    /// <summary>Uno de los valores de <see cref="Roles"/>.</summary>
    [FirestoreProperty]
    public string Rol { get; set; } = Roles.Visita;

    [FirestoreProperty]
    public bool Activo { get; set; } = true;

    /// <summary>Intentos de login fallidos seguidos.</summary>
    [FirestoreProperty]
    public int IntentosFallidos { get; set; }

    /// <summary>Si tiene valor y es futuro, la cuenta está bloqueada hasta esa fecha (UTC).</summary>
    [FirestoreProperty]
    public DateTime? BloqueadoHasta { get; set; }

    /// <summary>
    /// Cambia cada vez que cambia la clave. Va dentro de la cookie: si ya no coincide,
    /// esa sesión se cierra (así cambiar la clave saca a quien haya entrado con la anterior).
    /// Vacío por defecto para que los usuarios guardados antes de este campo lean siempre lo mismo.
    /// </summary>
    [FirestoreProperty]
    public string SelloSeguridad { get; set; } = string.Empty;

    /// <summary>Premios del camino de turnos que el Admin ya le entregó.</summary>
    [FirestoreProperty]
    public int PremiosEntregados { get; set; }

    /// <summary>Premios por los que ya se le mandó el correo de felicitación (evita repetirlo).</summary>
    [FirestoreProperty]
    public int PremiosNotificados { get; set; }

    [FirestoreProperty]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? UltimoIngreso { get; set; }

    public bool EstaBloqueado(DateTime ahoraUtc) => BloqueadoHasta is { } hasta && hasta > ahoraUtc;

    public static string Normalizar(string valor) => valor.Trim().ToLowerInvariant();

    public static string NuevoSello() => Guid.NewGuid().ToString("N");
}
