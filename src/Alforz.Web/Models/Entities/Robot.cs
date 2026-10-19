using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>
/// Robot del inventario (colección "robots"). Se puede elegir al agendar un turno.
/// La foto se guarda aparte, en la colección "imagenesRobots", con el mismo id.
/// </summary>
[FirestoreData]
public class Robot
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Precio en dólares.</summary>
    [FirestoreProperty]
    public double Precio { get; set; }

    /// <summary>Unidades disponibles en el inventario.</summary>
    [FirestoreProperty]
    public int Cantidad { get; set; }

    /// <summary>
    /// Cambia cada vez que se sube una foto nueva (sirve para que el navegador no muestre la vieja).
    /// Vacío = el robot no tiene foto.
    /// </summary>
    [FirestoreProperty]
    public string ImagenVersion { get; set; } = string.Empty;

    /// <summary>Si se ofrece al agendar y se ve en el inventario. Los robots no se borran: se ocultan.</summary>
    [FirestoreProperty]
    public bool Activo { get; set; } = true;

    [FirestoreProperty]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool TieneImagen => !string.IsNullOrEmpty(ImagenVersion);
}
