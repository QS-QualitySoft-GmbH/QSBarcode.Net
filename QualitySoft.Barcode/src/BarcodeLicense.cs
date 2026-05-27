using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Entry point for reading the native QS Barcode license state.
/// </summary>
public static class BarcodeLicense
{
    /// <summary>
    /// Reads the license status using the native engine search behavior.
    /// </summary>
    public static BarcodeLicenseStatus GetStatus()
    {
        return new BarcodeLicenseStatus(NativeMethods.Invoke(() => NativeMethods.qsbc_loader_license_status()));
    }

    /// <summary>
    /// Reads the license status from a specific license file.
    /// </summary>
    public static BarcodeLicenseStatus GetStatus(string? licenseFile)
    {
        var path = licenseFile;
        if (path == null || string.IsNullOrWhiteSpace(path))
        {
            return GetStatus();
        }

        return new BarcodeLicenseStatus(NativeMethods.Invoke(() => NativeMethods.qsbc_loader_license_status_file(NativeMethods.ToNullTerminatedUtf8(path))));
    }

    /// <summary>
    /// Attempts to read the license status without throwing native loading errors.
    /// </summary>
    public static bool TryGetStatus(out BarcodeLicenseStatus status, out string? error)
    {
        try
        {
            status = GetStatus();
            error = null;
            return true;
        }
        catch (Exception ex) when (NativeMethods.IsNativeBindingException(ex))
        {
            status = default;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Attempts to read the license status for a specific license file without throwing native loading errors.
    /// </summary>
    public static bool TryGetStatus(string? licenseFile, out BarcodeLicenseStatus status, out string? error)
    {
        try
        {
            status = GetStatus(licenseFile);
            error = null;
            return true;
        }
        catch (Exception ex) when (NativeMethods.IsNativeBindingException(ex))
        {
            status = default;
            error = ex.Message;
            return false;
        }
    }

}
