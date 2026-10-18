namespace Alforz.Web.Services.Interfaces;

/// <summary>
/// Envío de correos. Desacoplado del proveedor: hoy Brevo por SMTP,
/// mañana Gmail u otro cambiando solo la configuración.
/// </summary>
public interface IEmailService
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
}
