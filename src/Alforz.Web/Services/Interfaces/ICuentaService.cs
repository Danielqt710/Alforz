using System.Security.Claims;
using Alforz.Web.Models.Entities;

namespace Alforz.Web.Services.Interfaces;

public enum EstadoIngreso
{
    Correcto,
    CredencialesInvalidas,
    Bloqueado,
    Inactivo,
    CorreoSinVerificar
}

public record ResultadoIngreso(EstadoIngreso Estado, Usuario? Usuario = null, DateTime? BloqueadoHasta = null);

public record Resultado(bool Exito, string? Error = null)
{
    public static readonly Resultado Ok = new(true);
    public static Resultado Falla(string error) => new(false, error);
}

/// <summary>Registro, ingreso, verificación de correo y recuperación de clave.</summary>
public interface ICuentaService
{
    Task<(Resultado Resultado, Usuario? Usuario)> RegistrarAsync(string nombreUsuario, string correo, string clave);

    Task<ResultadoIngreso> IngresarAsync(string usuarioOCorreo, string clave);

    /// <summary>Genera un código nuevo y lo manda por correo. Devuelve false si se pidió otro hace muy poco.</summary>
    Task<bool> EnviarCodigoAsync(Usuario usuario, TipoToken tipo);

    Task<Resultado> VerificarCorreoAsync(string correo, string codigo);

    /// <summary>Si el correo existe, manda un código de recuperación. No revela si existe o no.</summary>
    Task SolicitarRecuperacionAsync(string correo);

    Task<Resultado> RestablecerClaveAsync(string correo, string codigo, string nuevaClave);

    ClaimsPrincipal CrearPrincipal(Usuario usuario);
}
