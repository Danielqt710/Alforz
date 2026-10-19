using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>
/// Foto de un robot (colección "imagenesRobots", id = id del robot).
/// Va en su propio documento para que listar robots no descargue todas las fotos.
/// Firestore admite documentos de hasta 1 MB, por eso las fotos se achican antes de subirse.
/// </summary>
[FirestoreData]
public class ImagenRobot
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public Blob Datos { get; set; }

    /// <summary>image/jpeg, image/png o image/webp.</summary>
    [FirestoreProperty]
    public string TipoContenido { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
}
