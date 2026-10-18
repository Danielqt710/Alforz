using System.Globalization;

namespace Alforz.Web.Helpers;

/// <summary>Precios con coma decimal, armados a mano (el servidor puede no tener la cultura "es").</summary>
public static class FormatoPrecio
{
    /// <summary>Ej.: "USD 5" o "USD 7,50".</summary>
    public static string Dolares(double precio) =>
        "USD " + (precio % 1 == 0
            ? precio.ToString("0", CultureInfo.InvariantCulture)
            : precio.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ','));
}
