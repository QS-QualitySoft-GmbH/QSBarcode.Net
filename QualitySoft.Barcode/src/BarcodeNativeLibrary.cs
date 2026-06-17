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
    /// Returns the native loader ABI major version.
    /// </summary>
    public static uint GetVersionMajor()
    {
        return NativeMethods.Invoke(() => NativeMethods.qsbc_loader_abi_version_major());
    }

    /// <summary>
    /// Returns the native loader ABI minor version.
    /// </summary>
    public static uint GetVersionMinor()
    {
        return NativeMethods.Invoke(() => NativeMethods.qsbc_loader_abi_version_minor());
    }

    /// <summary>
    /// Returns the native loader ABI patch version.
    /// </summary>
    public static uint GetVersionPatch()
    {
        return NativeMethods.Invoke(() => NativeMethods.qsbc_loader_abi_version_patch());
    }

    /// <summary>
    /// Returns the native QS Barcode engine version string.
    /// </summary>
    public static string GetEngineVersion()
    {
        return NativeMethods.Invoke(() => NativeMethods.PtrToString(NativeMethods.qsbc_loader_engine_version_string())) ?? "unknown";
    }

    /// <summary>
    /// Returns native loader capability bits.
    /// </summary>
    public static BarcodeNativeCapabilities GetCapabilities()
    {
        return (BarcodeNativeCapabilities)NativeMethods.Invoke(() => NativeMethods.qsbc_loader_capabilities());
    }

    /// <summary>
    /// Checks whether the native runtime supports the specified input format.
    /// </summary>
    public static bool IsFormatSupported(BarcodeImageFormat format)
    {
        return NativeMethods.Invoke(() => NativeMethods.qsbc_loader_is_format_supported((int)format)) != 0;
    }

    /// <summary>
    /// Returns the native display name for an input format.
    /// </summary>
    public static string GetFormatName(BarcodeImageFormat format)
    {
        return NativeMethods.Invoke(() => NativeMethods.PtrToString(NativeMethods.qsbc_loader_format_name((int)format))) ?? "unknown";
    }

    /// <summary>
    /// Returns the native display name for a status code.
    /// </summary>
    public static string GetStatusName(int status)
    {
        return NativeMethods.Invoke(() => NativeMethods.PtrToString(NativeMethods.qsbc_loader_status_name(status))) ?? "unknown";
    }

    /// <summary>
    /// Checks whether a native status code represents an error.
    /// </summary>
    public static bool IsErrorStatus(int status)
    {
        return NativeMethods.Invoke(() => NativeMethods.qsbc_loader_status_is_error(status)) != 0;
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
    /// Checks whether the native engine version can be read by the current process.
    /// </summary>
    public static bool TryGetEngineVersion(out string? version, out string? error)
    {
        try
        {
            version = GetEngineVersion();
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
