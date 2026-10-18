using System.Security.Cryptography;
using System.Text;

namespace Alforz.Web.Helpers;

/// <summary>
/// Genera los códigos de 6 dígitos que se mandan por correo y los compara
/// contra el hash guardado en Firestore.
/// </summary>
public static class TokenHelper
{
    public const int LargoCodigo = 6;

    /// <summary>Código numérico aleatorio criptográficamente seguro, ej. "048213".</summary>
    public static string GenerarCodigo()
    {
        var numero = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, LargoCodigo));
        return numero.ToString().PadLeft(LargoCodigo, '0');
    }

    /// <summary>SHA-256 en hexadecimal. Alcanza porque el código vence rápido y tiene intentos limitados.</summary>
    public static string Hashear(string codigo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo.Trim()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Compara en tiempo constante para no filtrar información por el tiempo de respuesta.</summary>
    public static bool Coincide(string codigoIngresado, string hashGuardado)
    {
        if (string.IsNullOrWhiteSpace(codigoIngresado) || string.IsNullOrEmpty(hashGuardado))
            return false;

        var a = Encoding.ASCII.GetBytes(Hashear(codigoIngresado));
        var b = Encoding.ASCII.GetBytes(hashGuardado);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
