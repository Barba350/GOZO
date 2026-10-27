namespace GOZO;
public static class ProductPhoto
{
    public const int MaxBytes = 40000;
    public static byte[]? Decode(string? encoded) {
        if (encoded == null) return null;
        if (encoded.Length > 54000) throw new RuleException("La foto optimizada supera 40 KB. Elige otra imagen.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(encoded); }
        catch (FormatException) { throw new RuleException("La foto no es válida. Vuelve a seleccionarla."); }
        // El navegador envía únicamente JPEG redimensionado, sin nombre de archivo ni rutas.
        if (bytes.Length is < 12 or > MaxBytes || bytes[0] != 255 || bytes[1] != 216 || bytes[^2] != 255 || bytes[^1] != 217)
            throw new RuleException("La foto debe ser una imagen JPEG optimizada válida.");
        bool dimensions = false;
        for (int i = 2; i + 3 < bytes.Length;) {
            if (bytes[i++] != 255) break;
            while (i < bytes.Length && bytes[i] == 255) i++;
            if (i >= bytes.Length) break;
            int marker = bytes[i++];
            if (marker is 217 or 218) break;
            if (marker == 1 || marker is >= 208 and <= 215) continue;
            if (i + 1 >= bytes.Length) break;
            int length = (bytes[i] << 8) | bytes[i + 1];
            if (length < 2 || i + length > bytes.Length) break;
            if (marker is 192 or 193 or 194) {
                if (length < 8) break;
                int height = (bytes[i + 3] << 8) | bytes[i + 4], width = (bytes[i + 5] << 8) | bytes[i + 6];
                if (width is < 1 or > 800 || height is < 1 or > 800) throw new RuleException("La foto debe medir como máximo 800 × 800 píxeles.");
                dimensions = true; break;
            }
            i += length;
        }
        if (!dimensions) throw new RuleException("No se reconocen las dimensiones de la foto JPEG.");
        return bytes;
    }
}
