using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alforz.Web.Controllers;

/// <summary>La gestión de robots se mudó a Inventario. Queda solo para que los links viejos sigan andando.</summary>
[Authorize]
public class RobotsController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Inventario");

    [HttpGet]
    public IActionResult Editar(string id) => RedirectToAction("Editar", "Inventario", new { id });
}
