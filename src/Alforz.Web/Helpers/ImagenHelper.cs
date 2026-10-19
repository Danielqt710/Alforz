namespace Alforz.Web.Helpers;

/// <summary>Validación de las fotos que se suben al inventario.</summary>
public static class ImagenHelper
{
    /// <summary>
    /// Tamaño máximo guardado. El navegador achica la foto antes de subirla (queda en ~200 KB),
    /// esto es solo el tope por si llega una sin achicar. Firestore no admite documentos de más de 1 MB.
    /// </summary>
    public const int MaxBytes = 900 * 1024;

    /// <summary>Tope del cuerpo de la petición, para cortar subidas enormes antes de leerlas.</summary>
    public const int MaxPeticionBytes = 8 * 1024 * 1024;

    /// <summary>
    /// Mira los primeros bytes del archivo (no la extensión ni lo que diga el navegador)
    /// y devuelve el tipo si es JPEG, PNG o WebP; si no, null.
    /// </summary>
    public static string? DetectarTipo(ReadOnlySpan<byte> datos)
    {
        if (datos.Length >= 3 && datos[0] == 0xFF && datos[1] == 0xD8 && datos[2] == 0xFF)
            return "image/jpeg";

        if (datos.Length >= 8 && datos[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";

        if (datos.Length >= 12
            && datos[..4].SequenceEqual("RIFF"u8)
            && datos.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }

    public static string NuevaVersion() => Guid.NewGuid().ToString("N")[..12];
}
