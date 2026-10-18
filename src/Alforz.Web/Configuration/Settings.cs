namespace Alforz.Web.Configuration;

/// <summary>Sección "Firebase" de appsettings. La ruta a la clave va en user-secrets, nunca en el repo.</summary>
public class FirebaseSettings
{
    public const string Seccion = "Firebase";

    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Ruta local al JSON de la cuenta de servicio.</summary>
    public string CredentialsPath { get; set; } = string.Empty;
}

/// <summary>Sección "Email" de appsettings (Brevo por SMTP). Usuario y clave van en user-secrets.</summary>
public class EmailSettings
{
    public const string Seccion = "Email";

    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Alforz";
}

/// <summary>Sección "Seguridad": bloqueos y vencimientos.</summary>
public class SeguridadSettings
{
    public const string Seccion = "Seguridad";

    public int MaxIntentosFallidos { get; set; } = 5;
    public int MinutosBloqueo { get; set; } = 15;
    public int MinutosValidezToken { get; set; } = 15;
    public int MaxIntentosToken { get; set; } = 5;
}
