using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>Una línea de log guardada en la colección "logs" (ver Logging/FirestoreLoggerProvider).</summary>
[FirestoreData]
public class RegistroLog
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime Fecha { get; set; }

    /// <summary>Information, Warning, Error o Critical.</summary>
    [FirestoreProperty]
    public string Nivel { get; set; } = string.Empty;

    /// <summary>Clase que escribió el log, ej. "Alforz.Web.Services.TurnoService".</summary>
    [FirestoreProperty]
    public string Categoria { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Mensaje { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Excepcion { get; set; } = string.Empty;

    /// <summary>Production (Render) o Development (una PC), para no mezclar pruebas con lo real.</summary>
    [FirestoreProperty]
    public string Origen { get; set; } = string.Empty;
}
