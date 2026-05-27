using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Legacy orientation mask. Use <see cref="All"/> or <see cref="Default"/> unless a scan should be constrained.
/// </summary>
[Flags]
public enum BarcodeOrientation
{
    Default = 0,
    Degrees0 = 0x00000001,
    Degrees90 = 0x00000002,
    Degrees180 = 0x00000004,
    Degrees270 = 0x00000008,
    Degrees0And180 = 0x00000010,
    Degrees90And270 = 0x00000020,
    All = Degrees0 | Degrees90 | Degrees180 | Degrees270
}

