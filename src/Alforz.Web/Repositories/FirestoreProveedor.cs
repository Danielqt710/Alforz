using Alforz.Web.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Repositories;

/// <summary>
/// Crea la conexión a Firestore la primera vez que un repositorio la usa,
/// así las páginas levantan aunque todavía falten los secretos de Firebase.
/// </summary>
public class FirestoreProveedor(IOptions<FirebaseSettings> opciones)
{
    private readonly Lazy<FirestoreDb> _db = new(() => Crear(opciones.Value));

    public FirestoreDb Db => _db.Value;

    private static FirestoreDb Crear(FirebaseSettings config)
    {
        if (!config.EstaConfigurado)
            throw new InvalidOperationException("Falta Firebase:ProjectId. Cárgalo con dotnet user-secrets (ver README).");

        var constructor = new FirestoreDbBuilder { ProjectId = config.ProjectId };
        // Sin ruta usa las credenciales por defecto de Google (GOOGLE_APPLICATION_CREDENTIALS, gcloud, etc.).
        if (!string.IsNullOrWhiteSpace(config.CredentialsPath))
            constructor.GoogleCredential = CredentialFactory
                .FromFile<ServiceAccountCredential>(config.CredentialsPath)
                .ToGoogleCredential();
        return constructor.Build();
    }
}
