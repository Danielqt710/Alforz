using Alforz.Web.Configuration;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Services;

/// <summary>
/// Al arrancar, si el catálogo está vacío, carga las 4 series de Alforz:
/// la más barata a 5 dólares y cada categoría siguiente 3 dólares más.
/// Después el Admin las cambia desde la sección Robots.
/// </summary>
public static class SeedRobots
{
    private static readonly (string Nombre, string Descripcion)[] Series =
    [
        ("Serie H · Hogar", "Compañeros domésticos que aprenden tu rutina."),
        ("Serie L · Logística", "Flotas autónomas para almacenes y última milla."),
        ("Serie M · Médica", "Asistentes quirúrgicos y de rehabilitación."),
        ("Serie A · Industrial", "Brazos de ensamblaje con precisión de 0,02 mm.")
    ];

    private const double PrecioInicial = 5;
    private const double Aumento = 3;

    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var proveedor = scope.ServiceProvider;
        if (!proveedor.GetRequiredService<IOptions<FirebaseSettings>>().Value.EstaConfigurado)
            return;

        var robots = proveedor.GetRequiredService<IRobotRepository>();
        if (await robots.HayAlgunoAsync())
            return;

        for (var i = 0; i < Series.Length; i++)
        {
            await robots.CrearAsync(new Robot
            {
                Nombre = Series[i].Nombre,
                Descripcion = Series[i].Descripcion,
                Precio = PrecioInicial + i * Aumento
            });
        }

        proveedor.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedRobots))
            .LogInformation("Catálogo de robots inicial creado ({Cantidad} robots)", Series.Length);
    }
}
