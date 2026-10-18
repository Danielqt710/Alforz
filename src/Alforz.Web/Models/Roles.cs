namespace Alforz.Web.Models;

/// <summary>
/// Niveles de usuario de Alforz. Se usan en [Authorize(Roles = ...)] y en las vistas.
/// </summary>
public static class Roles
{
    /// <summary>Control total: usuarios, roles, configuración y contenido.</summary>
    public const string Admin = "Admin";

    /// <summary>Crea y edita contenido; no gestiona usuarios.</summary>
    public const string Coordinador = "Coordinador";

    /// <summary>Solo lectura. Es el rol de toda cuenta nueva del registro público.</summary>
    public const string Visita = "Visita";

    /// <summary>Roles que pueden crear contenido (para usar en [Authorize]).</summary>
    public const string PuedenCrear = Admin + "," + Coordinador;

    public static readonly IReadOnlyList<string> Todos = [Admin, Coordinador, Visita];

    public static bool EsValido(string? rol) => rol is not null && Todos.Contains(rol);
}
