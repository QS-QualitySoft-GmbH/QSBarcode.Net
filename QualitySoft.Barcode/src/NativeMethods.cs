using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace QualitySoft.Barcode;

internal static class NativeMethods
{
    internal const int StatusMiss = 0;
    internal const int StatusHit = 1;
    internal const int StatusLicenseRequired = -6;

    private const string LibraryName = "qs_barcode_loader_sdk";
    private const string WindowsLibraryFileName = "qs_barcode_loader_sdk.dll";
    private const string LinuxLibraryFileName = "libqs_barcode_loader_sdk.so";
    private const string MacLibraryFileName = "libqs_barcode_loader_sdk.dylib";

#if NET5_0_OR_GREATER
    static NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveNativeLibrary);
    }
#endif

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_license_status();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_license_status_file(byte[] licenseFile);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_scan_options_init(ref NativeScanOptions options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_detect_file_format(byte[] path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_detect_image_format(IntPtr bytes, UIntPtr byteLen);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qsbc_loader_format_name(int format);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qsbc_loader_status_name(int status);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_status_is_error(int status);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_scan_file_cb_with_options(
        byte[] path,
        ref NativeScanOptions options,
        ResultCallback callback,
        IntPtr userData);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_scan_image_memory_cb_with_options(
        IntPtr bytes,
        UIntPtr byteLen,
        ref NativeScanOptions options,
        ResultCallback callback,
        IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ResultCallback(IntPtr result, IntPtr userData);

    internal static string? PtrToString(IntPtr value)
    {
        return value == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(value);
    }

    internal static byte[] ToNullTerminatedUtf8(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var text = Encoding.UTF8.GetBytes(value);
        var result = new byte[text.Length + 1];
        Buffer.BlockCopy(text, 0, result, 0, text.Length);
        return result;
    }

#if NET5_0_OR_GREATER
    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        foreach (var candidate in GetNativeLibraryCandidates())
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static string[] GetNativeLibraryCandidates()
    {
        var configured = Environment.GetEnvironmentVariable("QSBC_NATIVE_LIBRARY");
        var baseDirectory = AppContext.BaseDirectory;
        var libraryFileName = GetNativeLibraryFileName();
        var runtimeIdentifier = GetRuntimeIdentifier();

        return string.IsNullOrWhiteSpace(configured)
            ? new[]
            {
                Path.Combine(baseDirectory, libraryFileName),
                Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native", libraryFileName),
                Path.Combine(Environment.CurrentDirectory, libraryFileName),
                FindRepoNativeLibrary(baseDirectory),
                FindRepoNativeLibrary(Environment.CurrentDirectory)
            }
            : new[]
            {
                configured,
                Path.Combine(baseDirectory, libraryFileName),
                Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native", libraryFileName),
                Path.Combine(Environment.CurrentDirectory, libraryFileName),
                FindRepoNativeLibrary(baseDirectory),
                FindRepoNativeLibrary(Environment.CurrentDirectory)
            };
    }

    private static string GetNativeLibraryFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsLibraryFileName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacLibraryFileName;
        }

        return LinuxLibraryFileName;
    }

    private static string GetRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.ProcessArchitecture;
        var architectureName = architecture == Architecture.Arm64 ? "arm64" : "x64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-" + architectureName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-" + architectureName;
        }

        return "linux-" + architectureName;
    }

    private static string FindRepoNativeLibrary(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        var libraryFileName = GetNativeLibraryFileName();

        for (var i = 0; directory != null && i < 8; i++)
        {
            var candidate = Path.Combine(directory.FullName, "zig-out", "bin", libraryFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }
#endif
}
