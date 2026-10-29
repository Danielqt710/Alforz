using System.ComponentModel.DataAnnotations;
using Alforz.Web.Models.Entities;

namespace Alforz.Web.Models.ViewModels;

/// <summary>Formulario para agregar o editar un robot del inventario.</summary>
public class RobotFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Escribe el nombre del robot.")]
    [StringLength(60, ErrorMessage = "Hasta 60 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Hasta 200 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Escribe el precio.")]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "El precio tiene que ser mayor a 0.")]
    [Display(Name = "Precio (USD)")]
    public decimal? Precio { get; set; }

    [Required(ErrorMessage = "Escribe cuántos hay.")]
    [Range(0, 100000, ErrorMessage = "Entre 0 y 100.000.")]
    [Display(Name = "Cantidad en inventario")]
    public int? Cantidad { get; set; }

    [Display(Name = "Foto")]
    public IFormFile? Imagen { get; set; }

    /// <summary>Tildado al editar: borra la foto actual.</summary>
    [Display(Name = "Quitar la foto actual")]
    public bool QuitarImagen { get; set; }

    /// <summary>Solo para mostrar la foto actual al editar.</summary>
    public string ImagenVersion { get; set; } = string.Empty;

    public bool EsNuevo => string.IsNullOrEmpty(Id);

    public static RobotFormViewModel Desde(Robot robot) => new()
    {
        Id = robot.Id,
        Nombre = robot.Nombre,
        Descripcion = robot.Descripcion,
        Precio = (decimal)robot.Precio,
        Cantidad = robot.Cantidad,
        ImagenVersion = robot.ImagenVersion
    };
}

public class InventarioViewModel
{
    public IReadOnlyList<Robot> Robots { get; set; } = [];

    /// <summary>Admin o Coordinador: ven los ocultos, agregan y editan.</summary>
    public bool PuedeEditar { get; set; }

    /// <summary>Solo Admin oculta o muestra robots.</summary>
    public bool EsAdmin { get; set; }
}

/// <summary>Historial de cantidad y precio de un robot.</summary>
public class HistorialStockViewModel
{
    public Robot Robot { get; set; } = new();
    public IReadOnlyList<MovimientoStock> Movimientos { get; set; } = [];
    public TimeZoneInfo Zona { get; set; } = TimeZoneInfo.Utc;

    public DateTime HoraLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);
}
