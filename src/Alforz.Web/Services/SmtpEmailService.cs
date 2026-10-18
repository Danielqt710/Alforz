using Alforz.Web.Configuration;
using Alforz.Web.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Alforz.Web.Services;

/// <summary>
/// Envío por SMTP con MailKit (Brevo por defecto). Si en Development faltan las
/// credenciales SMTP, el correo se escribe en la consola para poder probar sin Brevo.
/// </summary>
public class SmtpEmailService(
    IOptions<EmailSettings> opciones,
    IWebHostEnvironment entorno,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _config = opciones.Value;

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        if (!_config.EstaConfigurado)
        {
            if (!entorno.IsDevelopment())
                throw new InvalidOperationException("Falta configurar el SMTP (sección Email).");

            logger.LogWarning("SMTP sin configurar. Correo NO enviado a {Destinatario}: {Asunto}\n{Cuerpo}",
                destinatario, asunto, cuerpoHtml);
            return;
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_config.SmtpHost, _config.SmtpPort, SecureSocketOptions.StartTls);
        await cliente.AuthenticateAsync(_config.SmtpUser, _config.SmtpPassword);
        await cliente.SendAsync(mensaje);
        await cliente.DisconnectAsync(true);
    }
}
