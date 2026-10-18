using Alforz.Web.Helpers;
using Alforz.Web.Models.Entities;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Alforz.Web.Controllers;

public class CuentaController(
    ICuentaService cuentas,
    IUsuarioRepository usuarios,
    ILogger<CuentaController> logger) : Controller
{
    private const string AvisoCodigoEnviado = "Si los datos son correctos, te enviamos un código a tu correo.";
    private const string CuentaSinValidar =
        "Tu cuenta todavía no está validada. Revisa tu casilla de correo (también Spam y Promociones) " +
        "y escribe acá el código que te enviamos.";
    private const string CodigoNuevo = "Te mandamos un código nuevo.";
    private const string CodigoReciente = "Ya te enviamos un código hace muy poco: búscalo en tu correo o espera un minuto para pedir otro.";
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
    [EnableRateLimiting(LimitesCuenta.Intentos)]
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
                var estadoCodigo = await IntentarEnviarCodigoAsync(resultado.Usuario!, TipoToken.VerificacionCorreo);
                TempData["Aviso"] = $"{CuentaSinValidar} {estadoCodigo}";
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
    [EnableRateLimiting(LimitesCuenta.Correos)]
    public async Task<IActionResult> Registrar(RegistrarViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            var registro = await cuentas.RegistrarAsync(modelo.NombreUsuario, modelo.Correo, modelo.Clave);
            if (!registro.Resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, registro.Resultado.Error!);
                return View(modelo);
            }

            if (registro.PendienteDeVerificar)
            {
                // Ya se había registrado con ese correo y no validó: en vez de "ya existe", que valide.
                TempData["Aviso"] = $"Ya tienes una cuenta con ese correo. {CuentaSinValidar} " +
                    (registro.CodigoReenviado ? CodigoNuevo : CodigoReciente);
            }
            else
            {
                TempData["Exito"] = "Cuenta creada. Te enviamos un código para verificar tu correo.";
            }
            return RedirectToAction(nameof(VerificarCorreo), new { correo = registro.Usuario!.Correo });
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
    [EnableRateLimiting(LimitesCuenta.Intentos)]
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
    [EnableRateLimiting(LimitesCuenta.Correos)]
    public async Task<IActionResult> ReenviarCodigo(string correo)
    {
        var usuario = string.IsNullOrWhiteSpace(correo) ? null : await usuarios.ObtenerPorCorreoAsync(correo);
        // Si se envió, el mismo mensaje que cuando la cuenta no existe, para no revelar qué correos están registrados.
        var aviso = AvisoCodigoEnviado;
        if (usuario is { CorreoVerificado: false, Activo: true })
        {
            var estado = await IntentarEnviarCodigoAsync(usuario, TipoToken.VerificacionCorreo);
            if (estado != CodigoNuevo)
                aviso = estado;
        }
        TempData["Aviso"] = aviso;

        return RedirectToAction(nameof(VerificarCorreo), new { correo });
    }

    // ---------- Recuperar clave ----------

    [HttpGet]
    public IActionResult OlvideClave() => View(new OlvideClaveViewModel());

    [HttpPost]
    [EnableRateLimiting(LimitesCuenta.Correos)]
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
    [EnableRateLimiting(LimitesCuenta.Intentos)]
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

    /// <summary>Manda un código y devuelve la frase que explica qué pasó (enviado, enviado hace poco o error).</summary>
    private async Task<string> IntentarEnviarCodigoAsync(Usuario usuario, TipoToken tipo)
    {
        try
        {
            return await cuentas.EnviarCodigoAsync(usuario, tipo) ? CodigoNuevo : CodigoReciente;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al enviar código {Tipo} a {UsuarioId}", tipo, usuario.Id);
            return ErrorCorreo;
        }
    }
}
