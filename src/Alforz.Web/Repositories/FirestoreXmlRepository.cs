using System.Xml.Linq;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Alforz.Web.Repositories;

/// <summary>
/// Guarda en Firestore las claves con las que ASP.NET firma cookies y tokens antiforgery.
/// Así sobreviven a cada nuevo deploy en Render y las sesiones no se cierran al publicar.
/// </summary>
public class FirestoreXmlRepository(FirestoreProveedor firestore) : IXmlRepository
{
    public const string Coleccion = "dataProtectionKeys";

    private CollectionReference Claves => firestore.Db.Collection(Coleccion);

    // La interfaz es sincrónica; se llama al arrancar y cuando rota una clave (cada 90 días).
    public IReadOnlyCollection<XElement> GetAllElements() =>
        Claves.GetSnapshotAsync().GetAwaiter().GetResult().Documents
            .Select(d => XElement.Parse(d.GetValue<string>("Xml")))
            .ToList();

    public void StoreElement(XElement element, string friendlyName) =>
        Claves.Document(friendlyName).SetAsync(new Dictionary<string, object>
        {
            ["Xml"] = element.ToString(SaveOptions.DisableFormatting),
            ["FechaCreacion"] = DateTime.UtcNow
        }).GetAwaiter().GetResult();
}
