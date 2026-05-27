using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Native runtime diagnostics for deployments and health checks.
/// </summary>
public static class BarcodeNativeLibrary
{
    /// <summary>
    /// Returns the native loader ABI version string.
    /// </summary>
    public static string GetVersion()
    {
        return NativeMethods.Invoke(() => NativeMethods.PtrToString(NativeMethods.qsbc_loader_abi_version_string())) ?? "unknown";
    }

    /// <summary>
    /// Checks whether the native runtime can be loaded by the current process.
    /// </summary>
    public static bool TryGetVersion(out string? version, out string? error)
    {
        try
        {
            version = GetVersion();
            error = null;
            return true;
        }
        catch (Exception ex) when (NativeMethods.IsNativeBindingException(ex))
        {
            version = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Returns a deployment diagnostic with runtime identifier and native probing locations.
    /// </summary>
    public static string GetDiagnostics()
    {
        return NativeMethods.GetNativeLibraryDiagnostic();
    }
}
