using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Feature bits returned by the native QS Barcode license API.
/// </summary>
[Flags]
public enum BarcodeLicenseFeatures
{
    None = 0,
    Demo = 0x00000001,
    Linear = 0x00000002,
    Pdf417 = 0x00000004,
    DataMatrix = 0x00000008,
    Qr = 0x00000010,
    Aztec = 0x00000020
}
