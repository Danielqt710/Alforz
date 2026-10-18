namespace Alforz.Web.Helpers;

/// <summary>Nombres de las políticas de límite de pedidos (ver Program.cs) que usan los POST de Cuenta.</summary>
public static class LimitesCuenta
{
    /// <summary>Ingresar, verificar código y restablecer clave: 10 cada 5 minutos por IP.</summary>
    public const string Intentos = "intentos";

    /// <summary>Lo que manda correos (registro, reenvío, olvidé mi clave): 5 cada 15 minutos por IP.</summary>
    public const string Correos = "correos";

    /// <summary>IP real del visitante (detrás del proxy de Render la completa UseForwardedHeaders).</summary>
    public static string ClaveIp(HttpContext contexto) =>
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
}
