using System.Security.Claims;
using Alforz.Web.Models;
using Alforz.Web.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alforz.Web.Controllers;

/// <summary>Gestión de usuarios: solo Admin. Cambia roles y activa o desactiva cuentas.</summary>
[Authorize(Roles = Roles.Admin)]
public class UsuariosController(IUsuarioRepository usuarios, ILogger<UsuariosController> logger) : Controller
{
    private string IdActual => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index()
    {
        ViewData["IdActual"] = IdActual;
        return View(await usuarios.ListarAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CambiarRol(string id, string rol)
    {
        if (!Roles.EsValido(rol))
            return BadRequest();

        if (id == IdActual)
        {
            TempData["Aviso"] = "No puedes cambiar tu propio rol.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await usuarios.ObtenerPorIdAsync(id);
        if (usuario is null)
            return NotFound();

        usuario.Rol = rol;
        await usuarios.ActualizarAsync(usuario);
        logger.LogInformation("{Admin} cambió el rol de {UsuarioId} a {Rol}", User.Identity?.Name, id, rol);

        TempData["Exito"] = $"{usuario.NombreUsuario} ahora es {rol}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(string id)
    {
        if (id == IdActual)
        {
            TempData["Aviso"] = "No puedes desactivar tu propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await usuarios.ObtenerPorIdAsync(id);
        if (usuario is null)
            return NotFound();

        usuario.Activo = !usuario.Activo;
        await usuarios.ActualizarAsync(usuario);
        logger.LogInformation("{Admin} dejó a {UsuarioId} con Activo={Activo}", User.Identity?.Name, id, usuario.Activo);

        TempData["Exito"] = $"{usuario.NombreUsuario} quedó {(usuario.Activo ? "activo" : "desactivado")}.";
        return RedirectToAction(nameof(Index));
    }
}
