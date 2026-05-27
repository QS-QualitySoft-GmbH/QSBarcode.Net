using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Barcode symbologies accepted by the native QS Barcode scan mask.
/// </summary>
[Flags]
public enum BarcodeSymbology
{
    /// <summary>
    /// Lets the native SDK choose its built-in default symbology set.
    /// </summary>
    NativeDefault = 0,

    /// <summary>
    /// Alias for <see cref="NativeDefault"/>. The native loader treats a zero mask as its default scan mask.
    /// </summary>
    None = 0,

    /// <summary>Code 128.</summary>
    Code128 = 0x00000001,

    /// <summary>GS1-128 / EAN-128.</summary>
    Ean128 = 0x00000002,

    /// <summary>Code 39.</summary>
    Code39 = 0x00000004,

    /// <summary>Extended Code 39.</summary>
    Code39Ext = 0x00000008,

    Code32 = 0x00000010,
    Code11 = 0x00000020,
    I25 = 0x00000040,
    Industrial25 = 0x00000080,
    Iata25 = 0x00000100,
    Inverted25 = 0x00000200,
    Matrix25 = 0x00000400,
    Datalogic25 = 0x00000800,
    BcdMatrix25 = 0x00001000,
    Codabar = 0x00002000,
    Code93 = 0x00004000,
    Code93Ext = 0x00008000,
    Ean8 = 0x00010000,
    Ean13 = 0x00020000,
    Upca = 0x00040000,
    Upce = 0x00080000,
    Codablock = 0x00100000,
    Databar = 0x00200000,
    Pharma = 0x00400000,
    Patch = 0x00800000,
    DatabarOmni = 0x01000000,
    DatabarExpanded = 0x02000000,
    DatabarLimited = 0x04000000,
    DataMatrix = 0x08000000,
    Qr = 0x10000000,
    Aztec = 0x20000000,
    Pdf417 = 0x40000000,
    Postal = unchecked((int)0x80000000),

    /// <summary>
    /// All one-dimensional symbologies supported by the native scan mask.
    /// </summary>
    LinearMask =
        Code128 |
        Ean128 |
        Code39 |
        Code39Ext |
        Code32 |
        Code11 |
        I25 |
        Industrial25 |
        Iata25 |
        Inverted25 |
        Matrix25 |
        Datalogic25 |
        BcdMatrix25 |
        Codabar |
        Code93 |
        Code93Ext |
        Ean8 |
        Ean13 |
        Upca |
        Upce |
        Codablock |
        Databar |
        Pharma |
        Patch |
        DatabarOmni |
        DatabarExpanded |
        DatabarLimited,

    /// <summary>
    /// All two-dimensional symbologies supported by the native scan mask.
    /// </summary>
    TwoDimensionalMask = DataMatrix | Qr | Aztec | Pdf417,

    /// <summary>
    /// All currently exposed symbologies, including postal.
    /// </summary>
    All = LinearMask | TwoDimensionalMask | Postal,

    /// <summary>
    /// Managed approximation of the native default mask: all common linear and 2D barcodes, excluding pharma, patch and postal codes.
    /// </summary>
    Default = All & ~Pharma & ~Patch & ~Postal
}
