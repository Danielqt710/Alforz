using Alforz.Web.Models.Entities;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alforz.Web.Controllers;

public class CuentaController(
    ICuentaService cuentas,
    IUsuarioRepository usuarios,
    ILogger<CuentaController> logger) : Controller
{
    private const string AvisoCodigoEnviado = "Si los datos son correctos, te enviamos un código a tu correo.";
    private const int DiasRecordarme = 30;
    private const string ErrorCorreo ="No pudimos enviar el correo. Inténtalo de nuevo en unos minutos.";

    // ---------- Ingresar / Salir ----------

    [HttpGet]
    public IActionResult Ingresar(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new IngresarViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Ingresar(IngresarViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await cuentas.IngresarAsync(modelo.UsuarioOCorreo, modelo.Clave);
        switch (resultado.Estado)
        {
            case EstadoIngreso.Correcto:
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    cuentas.CrearPrincipal(resultado.Usuario!),
                    new AuthenticationProperties
                    {
                        IsPersistent = modelo.Recordarme,
                        // Sin "Recordarme" vale ExpireTimeSpan (8 h). La renovación deslizante conserva este plazo.
                        ExpiresUtc = modelo.Recordarme ? DateTimeOffset.UtcNow.AddDays(DiasRecordarme) : null
                    });
                return Url.IsLocalUrl(modelo.ReturnUrl)
                    ? LocalRedirect(modelo.ReturnUrl)
                    : RedirectToAction("Index", "Home");

            case EstadoIngreso.CorreoSinVerificar:
                await IntentarEnviarCodigoAsync(resultado.Usuario!, TipoToken.VerificacionCorreo);
                TempData["Aviso"] = $"Antes de entrar tienes que verificar tu correo. {TempData.Peek("Aviso")}";
                return RedirectToAction(nameof(VerificarCorreo), new { correo = resultado.Usuario!.Correo });

            case EstadoIngreso.Bloqueado:
                var minutos = Math.Max(1, (int)Math.Ceiling((resultado.BloqueadoHasta!.Value - DateTime.UtcNow).TotalMinutes));
                ModelState.AddModelError(string.Empty,
                    $"Demasiados intentos fallidos. Vuelve a intentarlo en {minutos} minuto(s) o recupera tu clave.");
                break;

            case EstadoIngreso.Inactivo:
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Habla con un administrador.");
                break;

            default:
                ModelState.AddModelError(string.Empty, "Usuario o clave incorrectos.");
                break;
        }

        return View(modelo);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ---------- Registro y verificación ----------

    [HttpGet]
    public IActionResult Registrar()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegistrarViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var (resultado, usuario) = await cuentas.RegistrarAsync(modelo.NombreUsuario, modelo.Correo, modelo.Clave);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return View(modelo);
            }

            TempData["Exito"] = "Cuenta creada. Te enviamos un código para verificar tu correo.";
            return RedirectToAction(nameof(VerificarCorreo), new { correo = usuario!.Correo });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Si falló solo el correo, la cuenta ya existe: puede pedir el código de nuevo.
            logger.LogError(ex, "Error al registrar {Correo}", modelo.Correo);
            TempData["Aviso"] = "La cuenta se creó, pero no pudimos enviar el código. Pide uno nuevo.";
            return RedirectToAction(nameof(VerificarCorreo), new { correo = modelo.Correo });
        }
    }

    [HttpGet]
    public IActionResult VerificarCorreo(string? correo = null) =>
        View(new VerificarCorreoViewModel { Correo = correo ?? string.Empty });

    [HttpPost]
    public async Task<IActionResult> VerificarCorreo(VerificarCorreoViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await cuentas.VerificarCorreoAsync(modelo.Correo, modelo.Codigo);
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error!);
            return View(modelo);
        }

        TempData["Exito"] = "Correo verificado. Ya puedes ingresar.";
        return RedirectToAction(nameof(Ingresar));
    }

    [HttpPost]
    public async Task<IActionResult> ReenviarCodigo(string correo)
    {
        var usuario = string.IsNullOrWhiteSpace(correo) ? null : await usuarios.ObtenerPorCorreoAsync(correo);
        if (usuario is { CorreoVerificado: false, Activo: true })
            await IntentarEnviarCodigoAsync(usuario, TipoToken.VerificacionCorreo);
        else
            TempData["Aviso"] = AvisoCodigoEnviado;

        return RedirectToAction(nameof(VerificarCorreo), new { correo });
    }

    // ---------- Recuperar clave ----------

    [HttpGet]
    public IActionResult OlvideClave() => View(new OlvideClaveViewModel());

    [HttpPost]
    public async Task<IActionResult> OlvideClave(OlvideClaveViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            await cuentas.SolicitarRecuperacionAsync(modelo.Correo);
            TempData["Aviso"] = AvisoCodigoEnviado;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al enviar código de recuperación");
            TempData["Aviso"] = ErrorCorreo;
        }

        return RedirectToAction(nameof(RestablecerClave), new { correo = modelo.Correo });
    }

    [HttpGet]
    public IActionResult RestablecerClave(string? correo = null) =>
        View(new RestablecerClaveViewModel { Correo = correo ?? string.Empty });

    [HttpPost]
    public async Task<IActionResult> RestablecerClave(RestablecerClaveViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await cuentas.RestablecerClaveAsync(modelo.Correo, modelo.Codigo, modelo.NuevaClave);
        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error!);
            return View(modelo);
        }

        TempData["Exito"] = "Listo, tu clave fue cambiada. Ya puedes ingresar.";
        return RedirectToAction(nameof(Ingresar));
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    private async Task IntentarEnviarCodigoAsync(Usuario usuario, TipoToken tipo)
    {
        try
        {
            TempData["Aviso"] = await cuentas.EnviarCodigoAsync(usuario, tipo)
                ? AvisoCodigoEnviado
                : "Ya te enviamos un código hace muy poco. Revisa tu correo (también spam) o espera un minuto.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al enviar código {Tipo} a {UsuarioId}", tipo, usuario.Id);
            TempData["Aviso"] = ErrorCorreo;
        }
    }
}
