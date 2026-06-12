using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private const string NativeLibraryEnvironmentVariable = "QSBC_NATIVE_LIBRARY";
    private const string PdfRenderWorkerEnvironmentVariable = "QSBC_PDF_RENDER_WORKER";
    private const string WindowsPdfRenderWorkerFileName = "qs_barcode_pdf_render_worker.exe";
    private const string UnixPdfRenderWorkerFileName = "qs_barcode_pdf_render_worker";
    private const int LocalDevelopmentSearchDepth = 16;

    static NativeMethods()
    {
#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveNativeLibrary);
#endif
        ConfigurePdfRenderWorker();
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_license_status();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint qsbc_loader_abi_version_major();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint qsbc_loader_abi_version_minor();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint qsbc_loader_abi_version_patch();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qsbc_loader_abi_version_string();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qsbc_loader_engine_version_string();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_set_pdf_render_worker_path(byte[] path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_warmup_pdf_render_workers(uint requestedWorkers);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint qsbc_loader_capabilities();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_is_format_supported(int format);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_license_status_file(byte[] licenseFile);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_scan_options_init(ref NativeScanOptions options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_detect_file_format(byte[] path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_detect_image_format(IntPtr bytes, UIntPtr byteLen);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_page_count_file(byte[] path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_page_count_image_memory(IntPtr bytes, UIntPtr byteLen);

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

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_scan_gray8_cb_with_options(
        IntPtr pixels,
        uint width,
        uint height,
        uint stride,
        ref NativeScanOptions options,
        ResultCallback callback,
        IntPtr userData);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_render_file_page_bmp_with_options(
        byte[] path,
        ref NativeScanOptions options,
        ref NativeImageBuffer output);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_render_image_memory_page_bmp_with_options(
        IntPtr bytes,
        UIntPtr byteLen,
        ref NativeScanOptions options,
        ref NativeImageBuffer output);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_render_file_page_gray8_with_options(
        byte[] path,
        ref NativeScanOptions options,
        ref NativeImageBuffer output);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qsbc_loader_render_image_memory_page_gray8_with_options(
        IntPtr bytes,
        UIntPtr byteLen,
        ref NativeScanOptions options,
        ref NativeImageBuffer output);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qsbc_loader_free_image_buffer(ref NativeImageBuffer buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ResultCallback(IntPtr result, IntPtr userData);

    internal static TResult Invoke<TResult>(Func<TResult> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (IsNativeBindingException(ex))
        {
            throw CreateNativeLibraryException(ex);
        }
    }

    internal static void Invoke(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (IsNativeBindingException(ex))
        {
            throw CreateNativeLibraryException(ex);
        }
    }

    internal static bool IsNativeBindingException(Exception ex)
    {
        return ex is BarcodeNativeLibraryException
            || ex is DllNotFoundException
            || ex is EntryPointNotFoundException
            || ex is BadImageFormatException;
    }

    internal static BarcodeNativeLibraryException CreateNativeLibraryException(Exception innerException)
    {
        if (innerException is BarcodeNativeLibraryException nativeException)
        {
            return nativeException;
        }

        return new BarcodeNativeLibraryException(
            "Unable to load the QS Barcode native runtime for " + GetRuntimeIdentifier() + ". " + GetNativeLibraryDiagnostic(),
            innerException);
    }

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

    private static void ConfigurePdfRenderWorker()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PdfRenderWorkerEnvironmentVariable)))
        {
            return;
        }

        foreach (var candidate in GetPdfRenderWorkerCandidates())
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                var fullPath = Path.GetFullPath(candidate);
                Environment.SetEnvironmentVariable(PdfRenderWorkerEnvironmentVariable, fullPath);
                TrySetPdfRenderWorkerPath(fullPath);
                return;
            }
        }
    }

    private static void TrySetPdfRenderWorkerPath(string path)
    {
        try
        {
            qsbc_loader_set_pdf_render_worker_path(ToNullTerminatedUtf8(path));
        }
        catch (Exception ex) when (IsNativeBindingException(ex))
        {
        }
    }

    internal static string[] GetPdfRenderWorkerCandidates()
    {
        var configured = Environment.GetEnvironmentVariable(PdfRenderWorkerEnvironmentVariable);
        var baseDirectory = AppContext.BaseDirectory;
        var workerFileName = GetPdfRenderWorkerFileName();
        var runtimeIdentifier = GetRuntimeIdentifier();

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(configured);
        }

        candidates.Add(Path.Combine(baseDirectory, workerFileName));
        candidates.Add(Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native", workerFileName));
        candidates.Add(Path.Combine(Environment.CurrentDirectory, workerFileName));
        candidates.Add(FindRepoNativeFile(baseDirectory, workerFileName));
        candidates.Add(FindRepoNativeFile(Environment.CurrentDirectory, workerFileName));
        candidates.Add(FindArtifactsNativeFile(baseDirectory, workerFileName));
        candidates.Add(FindArtifactsNativeFile(Environment.CurrentDirectory, workerFileName));
        candidates.Add(FindSdkNativeFile(baseDirectory, workerFileName));
        candidates.Add(FindSdkNativeFile(Environment.CurrentDirectory, workerFileName));

        return candidates.ToArray();
    }

    internal static string GetPdfRenderWorkerFileName()
    {
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsPdfRenderWorkerFileName
            : UnixPdfRenderWorkerFileName;
#else
        return WindowsPdfRenderWorkerFileName;
#endif
    }

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
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

    internal static string GetNativeLibraryDiagnostic()
    {
        var builder = new StringBuilder();
        builder.Append("Runtime identifier: ");
        builder.Append(GetRuntimeIdentifier());
        builder.Append(". Native library file: ");
        builder.Append(GetNativeLibraryFileName());
        builder.Append(". Set ");
        builder.Append(NativeLibraryEnvironmentVariable);
        builder.Append(" to an absolute native library path when deploying custom layouts. Probed paths: ");

        var candidates = GetNativeLibraryCandidates()
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        builder.Append(candidates.Length == 0 ? "none" : string.Join("; ", candidates));
        builder.Append('.');
        return builder.ToString();
    }

    internal static string[] GetNativeLibraryCandidates()
    {
        var configured = Environment.GetEnvironmentVariable(NativeLibraryEnvironmentVariable);
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
                FindRepoNativeLibrary(Environment.CurrentDirectory),
                FindArtifactsNativeLibrary(baseDirectory),
                FindArtifactsNativeLibrary(Environment.CurrentDirectory),
                FindSdkNativeLibrary(baseDirectory),
                FindSdkNativeLibrary(Environment.CurrentDirectory)
            }
            : new[]
            {
                configured,
                Path.Combine(baseDirectory, libraryFileName),
                Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native", libraryFileName),
                Path.Combine(Environment.CurrentDirectory, libraryFileName),
                FindRepoNativeLibrary(baseDirectory),
                FindRepoNativeLibrary(Environment.CurrentDirectory),
                FindArtifactsNativeLibrary(baseDirectory),
                FindArtifactsNativeLibrary(Environment.CurrentDirectory),
                FindSdkNativeLibrary(baseDirectory),
                FindSdkNativeLibrary(Environment.CurrentDirectory)
            };
    }

    internal static string GetNativeLibraryFileName()
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

    internal static string GetRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.ProcessArchitecture;
        var architectureName = architecture == Architecture.X86
            ? "x86"
            : architecture == Architecture.Arm64
                ? "arm64"
                : architecture == Architecture.X64
                    ? "x64"
                    : throw new PlatformNotSupportedException("Unsupported QS Barcode CPU architecture: " + architecture);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-" + architectureName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-" + architectureName;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-" + architectureName;
        }

        throw new PlatformNotSupportedException("Unsupported QS Barcode platform.");
    }

    private static string FindRepoNativeLibrary(string startDirectory)
    {
        return FindRepoNativeFile(startDirectory, GetNativeLibraryFileName());
    }

    private static string FindSdkNativeLibrary(string startDirectory)
    {
        return FindSdkNativeFile(startDirectory, GetNativeLibraryFileName());
    }
#endif

    private static string FindRepoNativeFile(string startDirectory, string fileName)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var i = 0; directory != null && i < LocalDevelopmentSearchDepth; i++)
        {
            var candidate = Path.Combine(directory.FullName, "zig-out", "bin", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }

    private static string FindArtifactsNativeLibrary(string startDirectory)
    {
        return FindArtifactsNativeFile(startDirectory, GetNativeLibraryFileName());
    }

    private static string FindArtifactsNativeFile(string startDirectory, string fileName)
    {
        var directory = new DirectoryInfo(startDirectory);
        var runtimeIdentifier = GetRuntimeIdentifier();

        for (var i = 0; directory != null && i < LocalDevelopmentSearchDepth; i++)
        {
            var candidate = Path.Combine(directory.FullName, "artifacts", "native-runtime", runtimeIdentifier, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }

    private static string FindSdkNativeFile(string startDirectory, string fileName)
    {
        var directory = new DirectoryInfo(startDirectory);
        var runtimeIdentifier = GetRuntimeIdentifier();

        for (var i = 0; directory != null && i < LocalDevelopmentSearchDepth; i++)
        {
            var candidate = Path.Combine(directory.FullName, "sdk", "dotnet", "native", runtimeIdentifier, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }
#if !(NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER)
    internal static string GetNativeLibraryDiagnostic()
    {
        return "Runtime identifier: " + GetRuntimeIdentifier() + ". Native library file: " + GetNativeLibraryFileName() + ". On .NET Framework, copy the native DLL next to the application or make it reachable through PATH. Set QSBC_NATIVE_LIBRARY only on .NET 5+ runtimes.";
    }

    internal static string[] GetNativeLibraryCandidates()
    {
        return new[]
        {
            Path.Combine(AppContext.BaseDirectory, GetNativeLibraryFileName()),
            Path.Combine(Environment.CurrentDirectory, GetNativeLibraryFileName())
        };
    }

    internal static string GetNativeLibraryFileName()
    {
        return WindowsLibraryFileName;
    }

    internal static string GetRuntimeIdentifier()
    {
        return IntPtr.Size == 4 ? "win-x86" : "win-x64";
    }
#endif
}
