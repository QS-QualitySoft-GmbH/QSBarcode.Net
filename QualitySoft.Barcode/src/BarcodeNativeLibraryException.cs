using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Raised when the managed wrapper cannot load or bind the native QS Barcode runtime library.
/// </summary>
public sealed class BarcodeNativeLibraryException : InvalidOperationException
{
    internal BarcodeNativeLibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
