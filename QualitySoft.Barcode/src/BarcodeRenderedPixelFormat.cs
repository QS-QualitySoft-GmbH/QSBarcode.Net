namespace QualitySoft.Barcode;

/// <summary>
/// Pixel representation returned by the native renderer.
/// </summary>
public enum BarcodeRenderedPixelFormat
{
    /// <summary>
    /// Complete 24-bit BMP file bytes.
    /// </summary>
    Bmp24 = 0,

    /// <summary>
    /// Tightly packed 8-bit grayscale pixels, one byte per pixel.
    /// </summary>
    Gray8 = 1
}
