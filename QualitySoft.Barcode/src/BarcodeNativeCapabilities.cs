using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Feature bits reported by the native QS Barcode loader runtime.
/// </summary>
[Flags]
public enum BarcodeNativeCapabilities : uint
{
    None = 0,
    MemoryScan = 1u << 0,
    FileScan = 1u << 1,
    CallbackScan = 1u << 2,
    MultiPageScan = 1u << 3,
    Gif = 1u << 8,
    Png = 1u << 9,
    Bmp = 1u << 10,
    Jpeg = 1u << 11,
    Pdf = 1u << 12,
    Tiff = 1u << 13
}
