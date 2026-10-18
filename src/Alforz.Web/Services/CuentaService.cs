using System.Net;
using System.Security.Claims;
using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Services;

public class CuentaService(
    IUsuarioRepository usuarios,
    ITokenRepository tokens,
    IEmailService correo,
    PasswordHelper claves,
    IOptions<SeguridadSettings> opciones,
    ILogger<CuentaService> logger) : ICuentaService
{
    /// <summary>Claim con la hora (ticks UTC) en que se revisó el usuario contra Firestore por última vez.</summary>
    public const string ClaimValidado = "alforz:validado";

    private const string ErrorCodigo = "El código no es válido o ya venció. Pide uno nuevo.";

    private readonly SeguridadSettings _seguridad = opciones.Value;

    public async Task<(Resultado Resultado, Usuario? Usuario)> RegistrarAsync(string nombreUsuario, string correoUsuario, string clave)
    {
        nombreUsuario = nombreUsuario.Trim();
        correoUsuario = correoUsuario.Trim();

        if (await usuarios.ObtenerPorNombreUsuarioAsync(nombreUsuario) is not null)
            return (Resultado.Falla("Ese nombre de usuario ya está en uso."), null);

        if (await usuarios.ObtenerPorCorreoAsync(correoUsuario) is not null)
            return (Resultado.Falla("Ya hay una cuenta con ese correo."), null);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreUsuarioNormalizado = Usuario.Normalizar(nombreUsuario),
            Correo = correoUsuario,
            CorreoNormalizado = Usuario.Normalizar(correoUsuario),
            Rol = Roles.Visita,
            FechaCreacion = DateTime.UtcNow
        };
        usuario.ClaveHash = claves.Hashear(usuario, clave);

        await usuarios.CrearAsync(usuario);
        logger.LogInformation("Usuario registrado {UsuarioId} ({Nombre})", usuario.Id, usuario.NombreUsuario);

        await EnviarCodigoAsync(usuario, TipoToken.VerificacionCorreo);
        return (Resultado.Ok, usuario);
    }

    public async Task<ResultadoIngreso> IngresarAsync(string usuarioOCorreo, string clave)
    {
        var ahora = DateTime.UtcNow;
        var usuario = usuarioOCorreo.Contains('@')
            ? await usuarios.ObtenerPorCorreoAsync(usuarioOCorreo)
            : await usuarios.ObtenerPorNombreUsuarioAsync(usuarioOCorreo);

        if (usuario is null)
            return new(EstadoIngreso.CredencialesInvalidas);

        if (usuario.EstaBloqueado(ahora))
            return new(EstadoIngreso.Bloqueado, BloqueadoHasta: usuario.BloqueadoHasta);

        var (correcta, requiereRehash) = claves.Verificar(usuario, clave);
        if (!correcta)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= _seguridad.MaxIntentosFallidos)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = ahora.AddMinutes(_seguridad.MinutosBloqueo);
                logger.LogWarning("Cuenta {UsuarioId} bloqueada por intentos fallidos", usuario.Id);
            }
            await usuarios.ActualizarAsync(usuario);
            return usuario.EstaBloqueado(ahora)
                ? new(EstadoIngreso.Bloqueado, BloqueadoHasta: usuario.BloqueadoHasta)
                : new(EstadoIngreso.CredencialesInvalidas);
        }

        // A partir de acá la clave es correcta: recién ahora se informa el estado de la cuenta.
        if (!usuario.Activo)
            return new(EstadoIngreso.Inactivo);

        if (!usuario.CorreoVerificado)
            return new(EstadoIngreso.CorreoSinVerificar, usuario);

        if (requiereRehash)
            usuario.ClaveHash = claves.Hashear(usuario, clave);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoIngreso = ahora;
        await usuarios.ActualizarAsync(usuario);

        return new(EstadoIngreso.Correcto, usuario);
    }

    public async Task<bool> EnviarCodigoAsync(Usuario usuario, TipoToken tipo)
    {
        var ahora = DateTime.UtcNow;
        var anterior = await tokens.ObtenerVigenteAsync(usuario.Id, tipo);
        if (anterior is not null && anterior.FechaCreacion > ahora.AddSeconds(-_seguridad.SegundosEntreReenvios))
            return false;

        await tokens.InvalidarAnterioresAsync(usuario.Id, tipo);

        var codigo = TokenHelper.GenerarCodigo();
        await tokens.CrearAsync(new TokenSeguridad
        {
            UsuarioId = usuario.Id,
            Tipo = tipo,
            CodigoHash = TokenHelper.Hashear(codigo),
            ExpiraEn = ahora.AddMinutes(_seguridad.MinutosValidezToken),
            FechaCreacion = ahora
        });

        var (asunto, titulo, texto) = tipo switch
        {
            TipoToken.VerificacionCorreo => ("Verifica tu correo en Alforz", "Verifica tu correo",
                "Usa este código para confirmar tu correo en Alforz:"),
            _ => ("Recupera tu clave de Alforz", "Recuperar clave",
                "Usa este código para crear una clave nueva en Alforz:")
        };
        await correo.EnviarAsync(usuario.Correo, asunto, ArmarCorreo(usuario.NombreUsuario, titulo, texto, codigo));
        return true;
    }

    public async Task<Resultado> VerificarCorreoAsync(string correoUsuario, string codigo)
    {
        var usuario = await usuarios.ObtenerPorCorreoAsync(correoUsuario);
        if (usuario is null)
            return Resultado.Falla(ErrorCodigo);

        if (usuario.CorreoVerificado)
            return Resultado.Ok;

        if (!await ConsumirCodigoAsync(usuario.Id, TipoToken.VerificacionCorreo, codigo))
            return Resultado.Falla(ErrorCodigo);

        usuario.CorreoVerificado = true;
        await usuarios.ActualizarAsync(usuario);
        return Resultado.Ok;
    }

    public async Task SolicitarRecuperacionAsync(string correoUsuario)
    {
        var usuario = await usuarios.ObtenerPorCorreoAsync(correoUsuario);
        if (usuario is null || !usuario.Activo)
            return;

        await EnviarCodigoAsync(usuario, TipoToken.RecuperacionClave);
    }

    public async Task<Resultado> RestablecerClaveAsync(string correoUsuario, string codigo, string nuevaClave)
    {
        var usuario = await usuarios.ObtenerPorCorreoAsync(correoUsuario);
        if (usuario is null || !usuario.Activo)
            return Resultado.Falla(ErrorCodigo);

        if (!await ConsumirCodigoAsync(usuario.Id, TipoToken.RecuperacionClave, codigo))
            return Resultado.Falla(ErrorCodigo);

        usuario.ClaveHash = claves.Hashear(usuario, nuevaClave);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        // Si recibió el código, el correo es suyo.
        usuario.CorreoVerificado = true;
        await usuarios.ActualizarAsync(usuario);
        logger.LogInformation("Clave restablecida para {UsuarioId}", usuario.Id);
        return Resultado.Ok;
    }

    public ClaimsPrincipal CrearPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, usuario.Rol),
            new(ClaimValidado, DateTime.UtcNow.Ticks.ToString())
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identidad);
    }

    /// <summary>Valida el código contra el último token vigente, contando intentos. Si coincide lo marca como usado.</summary>
    private async Task<bool> ConsumirCodigoAsync(string usuarioId, TipoToken tipo, string codigo)
    {
        var token = await tokens.ObtenerVigenteAsync(usuarioId, tipo);
        if (token is null)
            return false;

        if (!TokenHelper.Coincide(codigo, token.CodigoHash))
        {
            token.Intentos++;
            if (token.Intentos >= _seguridad.MaxIntentosToken)
                token.Usado = true;
            await tokens.ActualizarAsync(token);
            return false;
        }

        token.Usado = true;
        await tokens.ActualizarAsync(token);
        return true;
    }

    private string ArmarCorreo(string nombre, string titulo, string texto, string codigo) => $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;color:#2F3A4A;max-width:480px;margin:auto">
          <h2 style="background:#BFE3F2;padding:16px;border-radius:12px;margin:0 0 16px">{titulo}</h2>
          <p>Hola {WebUtility.HtmlEncode(nombre)},</p>
          <p>{texto}</p>
          <p style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#D6D1F5;padding:12px;border-radius:12px;text-align:center">{codigo}</p>
          <p>Vence en {_seguridad.MinutosValidezToken} minutos. Si no fuiste tú, ignora este correo.</p>
        </div>
        """;
}
