using System.Security.Claims;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Alforz.Web.Services;

/// <summary>
/// Cada pocos minutos revisa la cookie contra Firestore: si el usuario fue desactivado
/// o cambió su clave (el sello de seguridad no coincide) lo saca, y si le cambiaron el rol
/// actualiza la cookie sin que tenga que volver a entrar.
/// </summary>
public class ValidadorCookie(IUsuarioRepository usuarios, ICuentaService cuentas) : CookieAuthenticationEvents
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null)
            return;

        var ticks = principal.FindFirstValue(CuentaService.ClaimValidado);
        if (long.TryParse(ticks, out var valor) && DateTime.UtcNow - new DateTime(valor, DateTimeKind.Utc) < Intervalo)
            return;

        var usuario = await usuarios.ObtenerPorIdAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        var selloCookie = principal.FindFirstValue(CuentaService.ClaimSello) ?? string.Empty;
        if (usuario is null || !usuario.Activo || usuario.EstaBloqueado(DateTime.UtcNow) ||
            usuario.SelloSeguridad != selloCookie)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        context.ReplacePrincipal(cuentas.CrearPrincipal(usuario));
        context.ShouldRenew = true;
    }
}
