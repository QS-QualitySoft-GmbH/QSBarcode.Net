using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Optional native scan flags.
/// </summary>
[Flags]
public enum BarcodeScanFlags
{
    None = 0,
    DataMatrixReportSymbolIdentifier = 1 << 0,
    DataMatrixSuppressEci = 1 << 1,
    DataMatrixIntensiveSearch = 1 << 2,
    DataMatrixSearchOnDoubledRegion = 1 << 3,
    DataMatrixZebraDoubling = 1 << 4,
    DataMatrixTryErodedImage = 1 << 5,
    QrEci = 1 << 6,
    QrDoubleImage = 1 << 7
}

