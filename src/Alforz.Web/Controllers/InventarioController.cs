using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Controllers;

/// <summary>
/// Inventario de robots con foto.
/// Cualquier usuario logueado lo ve; Admin y Coordinador agregan, editan y ven el historial; solo Admin oculta o muestra.
/// </summary>
[Authorize]
public class InventarioController(
    IRobotRepository robots,
    IMovimientoStockRepository movimientos,
    IOptions<TurnosSettings> turnos,
    ILogger<InventarioController> logger) : Controller
{
    private bool PuedeEditar => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Coordinador);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var todos = await robots.ListarAsync();
        return View(new InventarioViewModel
        {
            Robots = PuedeEditar ? todos : todos.Where(r => r.Activo).ToList(),
            PuedeEditar = PuedeEditar,
            EsAdmin = User.IsInRole(Roles.Admin)
        });
    }

    /// <summary>
    /// Devuelve la foto. La URL lleva la versión (?v=...), así que el navegador la puede guardar
    /// un mes: si se sube otra foto cambia la URL.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 30 * 24 * 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Imagen(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        if (robot is null || (!robot.Activo && !PuedeEditar))
            return NotFound();

        var imagen = await robots.ObtenerImagenAsync(id);
        return imagen is null ? NotFound() : File(imagen.Datos.ByteString.ToByteArray(), imagen.TipoContenido);
    }

    [HttpGet]
    [Authorize(Roles = Roles.PuedenCrear)]
    public IActionResult Nuevo() => View("Formulario", new RobotFormViewModel { Cantidad = 1 });

    [HttpPost]
    [Authorize(Roles = Roles.PuedenCrear)]
    [RequestSizeLimit(ImagenHelper.MaxPeticionBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImagenHelper.MaxPeticionBytes)]
    public async Task<IActionResult> Nuevo(RobotFormViewModel modelo)
    {
        modelo.Id = null;
        var imagen = await LeerImagenAsync(modelo.Imagen);
        if (!ModelState.IsValid)
            return View("Formulario", modelo);

        var robot = new Robot
        {
            Nombre = modelo.Nombre.Trim(),
            Descripcion = modelo.Descripcion?.Trim() ?? string.Empty,
            Precio = Redondear(modelo.Precio!.Value),
            Cantidad = modelo.Cantidad!.Value
        };
        await robots.CrearAsync(robot);
        await RegistrarMovimientoAsync(robot, MovimientoStock.TipoAlta, cantidadAnterior: 0, precioAnterior: 0);

        if (imagen is not null)
        {
            await robots.GuardarImagenAsync(robot.Id, imagen.Value.Datos, imagen.Value.Tipo);
            robot.ImagenVersion = ImagenHelper.NuevaVersion();
            await robots.ActualizarAsync(robot);
        }

        logger.LogInformation("{Usuario} agregó el robot {RobotId} ({Nombre}, {Precio}, {Cantidad} u., foto: {TieneFoto})",
            User.Identity?.Name, robot.Id, robot.Nombre, robot.Precio, robot.Cantidad, robot.TieneImagen);

        TempData["Exito"] = $"Robot \"{robot.Nombre}\" agregado al inventario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.PuedenCrear)]
    public async Task<IActionResult> Editar(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        return robot is null ? NotFound() : View("Formulario", RobotFormViewModel.Desde(robot));
    }

    [HttpPost]
    [Authorize(Roles = Roles.PuedenCrear)]
    [RequestSizeLimit(ImagenHelper.MaxPeticionBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImagenHelper.MaxPeticionBytes)]
    public async Task<IActionResult> Editar(RobotFormViewModel modelo)
    {
        var robot = await robots.ObtenerPorIdAsync(modelo.Id ?? string.Empty);
        if (robot is null)
            return NotFound();

        var imagen = await LeerImagenAsync(modelo.Imagen);
        if (!ModelState.IsValid)
        {
            modelo.ImagenVersion = robot.ImagenVersion;
            return View("Formulario", modelo);
        }

        var cantidadAnterior = robot.Cantidad;
        var precioAnterior = robot.Precio;

        robot.Nombre = modelo.Nombre.Trim();
        robot.Descripcion = modelo.Descripcion?.Trim() ?? string.Empty;
        robot.Precio = Redondear(modelo.Precio!.Value);
        robot.Cantidad = modelo.Cantidad!.Value;

        if (imagen is not null)
        {
            await robots.GuardarImagenAsync(robot.Id, imagen.Value.Datos, imagen.Value.Tipo);
            robot.ImagenVersion = ImagenHelper.NuevaVersion();
        }
        else if (modelo.QuitarImagen && robot.TieneImagen)
        {
            await robots.BorrarImagenAsync(robot.Id);
            robot.ImagenVersion = string.Empty;
        }

        await robots.ActualizarAsync(robot);
        if (robot.Cantidad != cantidadAnterior || robot.Precio != precioAnterior)
            await RegistrarMovimientoAsync(robot, MovimientoStock.TipoCambio, cantidadAnterior, precioAnterior);

        logger.LogInformation("{Usuario} editó el robot {RobotId} ({Nombre}, {Precio}, {Cantidad} u., foto: {TieneFoto})",
            User.Identity?.Name, robot.Id, robot.Nombre, robot.Precio, robot.Cantidad, robot.TieneImagen);

        TempData["Exito"] = $"Robot \"{robot.Nombre}\" actualizado. Los turnos ya agendados conservan el precio que tenían.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Cambios de cantidad y precio de un robot, del más nuevo al más viejo.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.PuedenCrear)]
    public async Task<IActionResult> Historial(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        if (robot is null)
            return NotFound();

        return View(new HistorialStockViewModel
        {
            Robot = robot,
            Movimientos = await movimientos.ListarPorRobotAsync(robot.Id),
            Zona = ZonaHoraria.Buscar(turnos.Value.ZonaHoraria)
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CambiarEstado(string id)
    {
        var robot = await robots.ObtenerPorIdAsync(id);
        if (robot is null)
            return NotFound();

        robot.Activo = !robot.Activo;
        await robots.ActualizarAsync(robot);
        logger.LogInformation("{Usuario} {Accion} el robot {RobotId} ({Nombre})",
            User.Identity?.Name, robot.Activo ? "mostró" : "ocultó", robot.Id, robot.Nombre);

        TempData["Exito"] = robot.Activo
            ? $"\"{robot.Nombre}\" vuelve a verse en el inventario y al agendar."
            : $"\"{robot.Nombre}\" quedó oculto: las visitas no lo ven y no aparece al agendar.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lee y valida la foto subida. Si hay un problema lo agrega al ModelState y devuelve null.
    /// Si no se subió nada, devuelve null sin error.
    /// </summary>
    private async Task<(byte[] Datos, string Tipo)?> LeerImagenAsync(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return null;

        if (archivo.Length > ImagenHelper.MaxBytes)
        {
            ModelState.AddModelError(nameof(RobotFormViewModel.Imagen),
                $"La foto pesa demasiado (máximo {ImagenHelper.MaxBytes / 1024} KB). Prueba con otra o recórtala.");
            return null;
        }

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);
        var datos = memoria.ToArray();

        var tipo = ImagenHelper.DetectarTipo(datos);
        if (tipo is null)
        {
            ModelState.AddModelError(nameof(RobotFormViewModel.Imagen), "La foto tiene que ser JPG, PNG o WebP.");
            return null;
        }

        return (datos, tipo);
    }

    /// <summary>
    /// Guarda el cambio en el historial. Si falla, solo se registra el error: el robot ya quedó guardado
    /// y no tiene sentido mostrarle un error a quien lo editó.
    /// </summary>
    private async Task RegistrarMovimientoAsync(Robot robot, string tipo, int cantidadAnterior, double precioAnterior)
    {
        try
        {
            await movimientos.RegistrarAsync(new MovimientoStock
            {
                RobotId = robot.Id,
                RobotNombre = robot.Nombre,
                Usuario = User.Identity?.Name ?? string.Empty,
                Tipo = tipo,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = robot.Cantidad,
                PrecioAnterior = precioAnterior,
                PrecioNuevo = robot.Precio
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo guardar el historial del robot {RobotId}", robot.Id);
        }
    }

    private static double Redondear(decimal precio) => (double)Math.Round(precio, 2);
}
