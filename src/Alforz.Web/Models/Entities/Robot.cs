using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>Robot del catálogo (colección "robots") que se puede elegir al agendar un turno.</summary>
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

    /// <summary>Si se ofrece al agendar. Los robots no se borran: se ocultan.</summary>
    [FirestoreProperty]
    public bool Activo { get; set; } = true;

    [FirestoreProperty]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
