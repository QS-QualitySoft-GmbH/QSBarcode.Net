using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QualitySoft.Barcode;

/// <summary>
/// Default <see cref="IBarcodeReader"/> implementation backed by the native QS Barcode SDK.
/// </summary>
public sealed class QualitySoftBarcodeReader : IBarcodeReader
{
    private const int NativeScanThreadStackSize = 16 * 1024 * 1024;
    private static readonly NativeMethods.ResultCallback CollectCallback = CollectResult;

    public IReadOnlyList<BarcodeResult> Read(string path, BarcodeReaderOptions? options = null)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var nativeOptions = ToNativeOptions(options);
        var textEncoding = options?.TextEncoding;
        var requestedSymbologies = options?.Symbologies ?? BarcodeSymbology.NativeDefault;
        return ScanWithHandle(
            results => NativeMethods.qsbc_loader_scan_file_cb_with_options(
                NativeMethods.ToNullTerminatedUtf8(path),
                ref nativeOptions,
                CollectCallback,
                results),
            textEncoding,
            requestedSymbologies);
    }

    public IReadOnlyList<BarcodeResult> Read(FileInfo file, BarcodeReaderOptions? options = null)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return Read(file.FullName, options);
    }

    public IReadOnlyList<BarcodeResult> Read(byte[] bytes, BarcodeReaderOptions? options = null)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var nativeOptions = ToNativeOptions(options);
        var textEncoding = options?.TextEncoding;
        var requestedSymbologies = options?.Symbologies ?? BarcodeSymbology.NativeDefault;
        var bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return ScanWithHandle(
                results => NativeMethods.qsbc_loader_scan_image_memory_cb_with_options(
                    bytesHandle.AddrOfPinnedObject(),
                    (UIntPtr)bytes.Length,
                    ref nativeOptions,
                    CollectCallback,
                    results),
                textEncoding,
                requestedSymbologies);
        }
        finally
        {
            bytesHandle.Free();
        }
    }

    public IReadOnlyList<BarcodeResult> Read(Stream stream, BarcodeReaderOptions? options = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return Read(memory.ToArray(), options);
        }
    }

    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(string path, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return RunNativeScanAsync(() => Read(path, options), cancellationToken);
    }

    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(FileInfo file, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return ReadAsync(file.FullName, options, cancellationToken);
    }

    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(byte[] bytes, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return RunNativeScanAsync(() => Read(bytes, options), cancellationToken);
    }

    public async Task<IReadOnlyList<BarcodeResult>> ReadAsync(Stream stream, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await ReadAsync(memory.ToArray(), options, cancellationToken).ConfigureAwait(false);
        }
    }

    public BarcodeImageFormat DetectFormat(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return (BarcodeImageFormat)NativeMethods.qsbc_loader_detect_file_format(NativeMethods.ToNullTerminatedUtf8(path));
    }

    public BarcodeImageFormat DetectFormat(FileInfo file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return DetectFormat(file.FullName);
    }

    public BarcodeImageFormat DetectFormat(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (BarcodeImageFormat)NativeMethods.qsbc_loader_detect_image_format(bytesHandle.AddrOfPinnedObject(), (UIntPtr)bytes.Length);
        }
        finally
        {
            bytesHandle.Free();
        }
    }

    public BarcodeImageFormat DetectFormat(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return DetectFormat(memory.ToArray());
        }
    }

    private static NativeScanOptions ToNativeOptions(BarcodeReaderOptions? options)
    {
        return (options ?? new BarcodeReaderOptions()).ToNative();
    }

    private static Task<IReadOnlyList<BarcodeResult>> RunNativeScanAsync(Func<IReadOnlyList<BarcodeResult>> scan, CancellationToken cancellationToken)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        if (cancellationToken.IsCancellationRequested)
        {
#if NET462
            var canceled = new TaskCompletionSource<IReadOnlyList<BarcodeResult>>();
            canceled.SetCanceled();
            return canceled.Task;
#else
            return Task.FromCanceled<IReadOnlyList<BarcodeResult>>(cancellationToken);
#endif
        }

        var completion = new TaskCompletionSource<IReadOnlyList<BarcodeResult>>();
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult(scan());
            }
            catch (OperationCanceledException)
            {
                completion.TrySetCanceled();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }, NativeScanThreadStackSize)
        {
            IsBackground = true,
            Name = "QS Barcode native scan"
        };

        thread.Start();
        return completion.Task;
    }

    private static IReadOnlyList<BarcodeResult> ScanWithHandle(Func<IntPtr, int> scan, Encoding? textEncoding, BarcodeSymbology requestedSymbologies)
    {
        var state = new ScanCallbackState(textEncoding);
        var handle = GCHandle.Alloc(state);
        try
        {
            var status = scan(GCHandle.ToIntPtr(handle));
            if (status < 0)
            {
                throw CreateScanException(status, requestedSymbologies);
            }

            return state.Results;
        }
        finally
        {
            handle.Free();
        }
    }

    private static BarcodeScanException CreateScanException(int status, BarcodeSymbology requestedSymbologies)
    {
        var statusName = NativeMethods.PtrToString(NativeMethods.qsbc_loader_status_name(status));
        if (status != NativeMethods.StatusLicenseRequired)
        {
            return new BarcodeScanException(status, statusName);
        }

        try
        {
            var licenseStatus = BarcodeLicense.GetStatus();
            return new BarcodeScanException(
                status,
                statusName,
                requestedSymbologies,
                licenseStatus,
                licenseStatus.MissingFeaturesFor(requestedSymbologies));
        }
        catch
        {
            return new BarcodeScanException(
                status,
                statusName,
                requestedSymbologies,
                null,
                BarcodeLicenseFeatures.None);
        }
    }

    private static int CollectResult(IntPtr result, IntPtr userData)
    {
        if (result == IntPtr.Zero || userData == IntPtr.Zero)
        {
            return 1;
        }

        var handle = GCHandle.FromIntPtr(userData);
        var state = handle.Target as ScanCallbackState;
        if (state == null)
        {
            return 1;
        }

        var native = Marshal.PtrToStructure<NativeBarcodeResult>(result);
        var bytes = new byte[Math.Max(native.TextLen, 0)];

        if (native.Text != IntPtr.Zero && bytes.Length > 0)
        {
            Marshal.Copy(native.Text, bytes, 0, bytes.Length);
        }

        BarcodeBounds? bounds = null;
        if (native.HasBounds != 0)
        {
            bounds = new BarcodeBounds(native.BarcodeX, native.BarcodeY, native.BarcodeWidth, native.BarcodeHeight);
        }

        state.Results.Add(new BarcodeResult(
            (BarcodeImageFormat)native.Format,
            (BarcodeSymbology)native.SymbologyMask,
            bytes,
            native.Width,
            native.Height,
            native.ImageIndex,
            native.PageIndex,
            bounds,
            state.TextEncoding));

        return 0;
    }

    private sealed class ScanCallbackState
    {
        internal ScanCallbackState(Encoding? textEncoding)
        {
            TextEncoding = textEncoding;
        }

        internal List<BarcodeResult> Results { get; } = new List<BarcodeResult>();

        internal Encoding? TextEncoding { get; }
    }
}
