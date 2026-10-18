using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alforz.Web.Controllers;

/// <summary>Catálogo de robots: solo Admin. Agrega, edita precios y oculta o muestra robots.</summary>
[Authorize(Roles = Roles.Admin)]
public class RobotsController(IRobotRepository robots, ILogger<RobotsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(new RobotsViewModel { Robots = await robots.ListarAsync() });

    [HttpPost]
    public async Task<IActionResult> Crear([Bind(Prefix = nameof(RobotsViewModel.Nuevo))] RobotFormViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), new RobotsViewModel { Robots = await robots.ListarAsync(), Nuevo = modelo });

        var robot = new Robot
        {
            Nombre = modelo.Nombre.Trim(),
            Descripcion = modelo.Descripcion?.Trim() ?? string.Empty,
            Precio = Redondear(modelo.Precio!.Value)
        };
        await robots.CrearAsync(robot);
        logger.LogInformation("{Admin} agregó el robot {RobotId} ({Nombre}, {Precio})", User.Identity?.Name, robot.Id, robot.Nombre, robot.Precio);

        TempData["Exito"] = $"Robot \"{robot.Nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        return robot is null ? NotFound() : View(RobotFormViewModel.Desde(robot));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(RobotFormViewModel modelo)
    {
        var robot = await robots.ObtenerPorIdAsync(modelo.Id ?? string.Empty);
        if (robot is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(modelo);

        robot.Nombre = modelo.Nombre.Trim();
        robot.Descripcion = modelo.Descripcion?.Trim() ?? string.Empty;
        robot.Precio = Redondear(modelo.Precio!.Value);
        await robots.ActualizarAsync(robot);
        logger.LogInformation("{Admin} editó el robot {RobotId} ({Nombre}, {Precio})", User.Identity?.Name, robot.Id, robot.Nombre, robot.Precio);

        TempData["Exito"] = $"Robot \"{robot.Nombre}\" actualizado. Los turnos ya agendados conservan el precio que tenían.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        if (robot is null)
            return NotFound();

        robot.Activo = !robot.Activo;
        await robots.ActualizarAsync(robot);

        TempData["Exito"] = robot.Activo
            ? $"\"{robot.Nombre}\" vuelve a aparecer al agendar."
            : $"\"{robot.Nombre}\" quedó oculto: ya no aparece al agendar.";
        return RedirectToAction(nameof(Index));
    }

    private static double Redondear(decimal precio) => (double)Math.Round(precio, 2);
}
