using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Services;

/// <summary>
/// Al arrancar, si en Firestore no hay ningún Admin, crea el primero con los datos de "AdminInicial".
/// Si ya hay uno, no hace nada (no pisa claves ni roles).
/// </summary>
public static class SeedAdmin
{
    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var proveedor = scope.ServiceProvider;
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedAdmin));

        if (!proveedor.GetRequiredService<IOptions<FirebaseSettings>>().Value.EstaConfigurado)
        {
            logger.LogWarning("Firebase sin configurar: no se revisa el Admin inicial. Carga Firebase:ProjectId en user-secrets.");
            return;
        }

        var usuarios = proveedor.GetRequiredService<IUsuarioRepository>();
        if (await usuarios.ExisteAlgunAdminAsync())
            return;

        var config = proveedor.GetRequiredService<IOptions<AdminInicialSettings>>().Value;
        if (string.IsNullOrWhiteSpace(config.Correo) || string.IsNullOrWhiteSpace(config.Clave))
        {
            logger.LogWarning("No hay ningún Admin y faltan AdminInicial:Correo / AdminInicial:Clave en user-secrets.");
            return;
        }

        var existente = await usuarios.ObtenerPorCorreoAsync(config.Correo)
                        ?? await usuarios.ObtenerPorNombreUsuarioAsync(config.NombreUsuario);
        if (existente is not null)
        {
            // Ya existe esa cuenta (por ejemplo, se registró antes): se la asciende y se le pone
            // la clave de AdminInicial, para que nadie que haya registrado ese correo quede como Admin.
            existente.ClaveHash = proveedor.GetRequiredService<PasswordHelper>().Hashear(existente, config.Clave);
            existente.IntentosFallidos = 0;
            existente.BloqueadoHasta = null;
            existente.Rol = Roles.Admin;
            existente.Activo = true;
            existente.CorreoVerificado = true;
            await usuarios.ActualizarAsync(existente);
            logger.LogInformation("Usuario {Nombre} ascendido a Admin inicial", existente.NombreUsuario);
            return;
        }

        var admin = new Usuario
        {
            NombreUsuario = config.NombreUsuario.Trim(),
            NombreUsuarioNormalizado = Usuario.Normalizar(config.NombreUsuario),
            Correo = config.Correo.Trim(),
            CorreoNormalizado = Usuario.Normalizar(config.Correo),
            CorreoVerificado = true,
            Rol = Roles.Admin,
            FechaCreacion = DateTime.UtcNow
        };
        admin.ClaveHash = proveedor.GetRequiredService<PasswordHelper>().Hashear(admin, config.Clave);

        await usuarios.CrearAsync(admin);
        logger.LogInformation("Admin inicial {Nombre} creado", admin.NombreUsuario);
    }
}
