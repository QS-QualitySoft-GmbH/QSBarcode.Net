namespace QualitySoft.Barcode;

/// <summary>
/// Raw pixel layout accepted by managed raw-image scan overloads.
/// </summary>
public enum BarcodeRawPixelFormat
{
    Gray8 = 0,
    Rgb24 = 1,
    Bgr24 = 2,
    Rgba32 = 3,
    Bgra32 = 4
}
