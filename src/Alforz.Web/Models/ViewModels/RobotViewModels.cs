using System.ComponentModel.DataAnnotations;
using Alforz.Web.Models.Entities;

namespace Alforz.Web.Models.ViewModels;

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

    public static RobotFormViewModel Desde(Robot robot) => new()
    {
        Id = robot.Id,
        Nombre = robot.Nombre,
        Descripcion = robot.Descripcion,
        Precio = (decimal)robot.Precio
    };
}

public class RobotsViewModel
{
    public IReadOnlyList<Robot> Robots { get; set; } = [];
    public RobotFormViewModel Nuevo { get; set; } = new();
}
