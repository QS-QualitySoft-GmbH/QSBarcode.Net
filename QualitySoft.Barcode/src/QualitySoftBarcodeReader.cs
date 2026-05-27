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
public sealed class QualitySoftBarcodeReader : IBarcodeReader, IDisposable
{
    private const int StreamCopyBufferSize = 81920;
    private const int MaxManagedResultPayloadBytes = 64 * 1024 * 1024;
    private static readonly NativeMethods.ResultCallback CollectCallback = CollectResult;
    private readonly BarcodeReaderOptions _defaultOptions;
    private readonly SemaphoreSlim _nativeScanSlots;
    private readonly int _nativeScanThreadStackSize;
    private bool _disposed;

    /// <summary>
    /// Creates a reader using native SDK defaults.
    /// </summary>
    public QualitySoftBarcodeReader()
        : this(new BarcodeReaderSettings())
    {
    }

    /// <summary>
    /// Creates a reader with managed default options used whenever a read call does not pass options explicitly.
    /// </summary>
    public QualitySoftBarcodeReader(BarcodeReaderOptions defaultOptions)
        : this(CreateSettings(defaultOptions))
    {
    }

    /// <summary>
    /// Creates a reader with explicit managed defaults and native scan concurrency settings.
    /// </summary>
    public QualitySoftBarcodeReader(BarcodeReaderSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        settings.Validate();
        var snapshot = settings.Clone();
        _defaultOptions = snapshot.DefaultOptions;
        _nativeScanSlots = new SemaphoreSlim(snapshot.MaxConcurrentScans, snapshot.MaxConcurrentScans);
        _nativeScanThreadStackSize = snapshot.NativeScanThreadStackSize;
    }

    public IReadOnlyList<BarcodeResult> Read(string path, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePath(path, nameof(path));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() =>
        {
            var nativeOptions = ToNativeOptions(effectiveOptions);
            return ScanWithHandle(
                results => NativeMethods.qsbc_loader_scan_file_cb_with_options(
                    NativeMethods.ToNullTerminatedUtf8(path),
                    ref nativeOptions,
                    CollectCallback,
                    results),
                effectiveOptions.TextEncoding,
                effectiveOptions.Symbologies);
        });
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
        EnsureReadableBuffer(bytes, nameof(bytes));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => ReadBytesCore(bytes, effectiveOptions));
    }

    private IReadOnlyList<BarcodeResult> ReadBytesCore(byte[] bytes, BarcodeReaderOptions effectiveOptions)
    {
        var nativeOptions = ToNativeOptions(effectiveOptions);
        var bytesHandle = default(GCHandle);
        try
        {
            bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return ScanWithHandle(
                results => NativeMethods.qsbc_loader_scan_image_memory_cb_with_options(
                    bytesHandle.AddrOfPinnedObject(),
                    (UIntPtr)bytes.Length,
                    ref nativeOptions,
                    CollectCallback,
                    results),
                effectiveOptions.TextEncoding,
                effectiveOptions.Symbologies);
        }
        finally
        {
            if (bytesHandle.IsAllocated)
            {
                bytesHandle.Free();
            }
        }
    }

    public IReadOnlyList<BarcodeResult> Read(Stream stream, BarcodeReaderOptions? options = null)
    {
        EnsureReadableStream(stream, nameof(stream));

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory, StreamCopyBufferSize);
            return Read(memory.ToArray(), options);
        }
    }

    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(string path, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadablePath(path, nameof(path));
        var effectiveOptions = GetOptionsSnapshot(options);

        return RunNativeScanAsync(() =>
        {
            var nativeOptions = ToNativeOptions(effectiveOptions);
            return ScanWithHandle(
                results => NativeMethods.qsbc_loader_scan_file_cb_with_options(
                    NativeMethods.ToNullTerminatedUtf8(path),
                    ref nativeOptions,
                    CollectCallback,
                    results),
                effectiveOptions.TextEncoding,
                effectiveOptions.Symbologies);
        }, cancellationToken);
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
        EnsureReadableBuffer(bytes, nameof(bytes));
        var effectiveOptions = GetOptionsSnapshot(options);
        var bytesSnapshot = new byte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, bytesSnapshot, 0, bytes.Length);

        return RunNativeScanAsync(() => ReadBytesCore(bytesSnapshot, effectiveOptions), cancellationToken);
    }

    public async Task<IReadOnlyList<BarcodeResult>> ReadAsync(Stream stream, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadableStream(stream, nameof(stream));
        var effectiveOptions = GetOptionsSnapshot(options);

        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, StreamCopyBufferSize, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await ReadAsync(memory.ToArray(), effectiveOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    public BarcodeImageFormat DetectFormat(string path)
    {
        EnsureReadablePath(path, nameof(path));

        return (BarcodeImageFormat)NativeMethods.Invoke(() => NativeMethods.qsbc_loader_detect_file_format(NativeMethods.ToNullTerminatedUtf8(path)));
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
        EnsureReadableBuffer(bytes, nameof(bytes));

        var bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (BarcodeImageFormat)NativeMethods.Invoke(() => NativeMethods.qsbc_loader_detect_image_format(bytesHandle.AddrOfPinnedObject(), (UIntPtr)bytes.Length));
        }
        finally
        {
            bytesHandle.Free();
        }
    }

    public BarcodeImageFormat DetectFormat(Stream stream)
    {
        EnsureReadableStream(stream, nameof(stream));

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory, StreamCopyBufferSize);
            return DetectFormat(memory.ToArray());
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _nativeScanSlots.Dispose();
    }

    private BarcodeReaderOptions GetOptionsSnapshot(BarcodeReaderOptions? options)
    {
        ThrowIfDisposed();
        var snapshot = options == null ? _defaultOptions.Clone() : options.Clone();
        snapshot.Validate();
        return snapshot;
    }

    private static NativeScanOptions ToNativeOptions(BarcodeReaderOptions options)
    {
        return options.ToNative();
    }

    private IReadOnlyList<BarcodeResult> RunNativeScan(Func<IReadOnlyList<BarcodeResult>> scan)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        ThrowIfDisposed();
        _nativeScanSlots.Wait();
        try
        {
            ThrowIfDisposed();
            return scan();
        }
        finally
        {
            SafeReleaseNativeScanSlot();
        }
    }

    private async Task<IReadOnlyList<BarcodeResult>> RunNativeScanAsync(Func<IReadOnlyList<BarcodeResult>> scan, CancellationToken cancellationToken)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        ThrowIfDisposed();
        await _nativeScanSlots.WaitAsync(cancellationToken).ConfigureAwait(false);

        var completion = new TaskCompletionSource<IReadOnlyList<BarcodeResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationRegistration = default(CancellationTokenRegistration);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(state =>
            {
                var taskCompletion = (TaskCompletionSource<IReadOnlyList<BarcodeResult>>)state!;
#if NET462
                taskCompletion.TrySetCanceled();
#else
                taskCompletion.TrySetCanceled(cancellationToken);
#endif
            }, completion);
        }

        var thread = new Thread(() =>
        {
            try
            {
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult(scan());
            }
            catch (OperationCanceledException)
            {
#if NET462
                completion.TrySetCanceled();
#else
                completion.TrySetCanceled(cancellationToken);
#endif
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                cancellationRegistration.Dispose();
                SafeReleaseNativeScanSlot();
            }
        }, _nativeScanThreadStackSize)
        {
            IsBackground = true,
            Name = "QS Barcode native scan"
        };

        try
        {
            thread.Start();
        }
        catch
        {
            cancellationRegistration.Dispose();
            SafeReleaseNativeScanSlot();
            throw;
        }

        return await completion.Task.ConfigureAwait(false);
    }

    private static IReadOnlyList<BarcodeResult> ScanWithHandle(Func<IntPtr, int> scan, Encoding? textEncoding, BarcodeSymbology requestedSymbologies)
    {
        var state = new ScanCallbackState(textEncoding);
        var handle = GCHandle.Alloc(state);
        try
        {
            var status = NativeMethods.Invoke(() => scan(GCHandle.ToIntPtr(handle)));
            if (state.CallbackException != null)
            {
                throw new InvalidOperationException("Failed to marshal a native barcode result.", state.CallbackException);
            }

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

        try
        {
            var native = Marshal.PtrToStructure<NativeBarcodeResult>(result);
            if (native.TextLen < 0 || native.TextLen > MaxManagedResultPayloadBytes)
            {
                state.CallbackException = new InvalidOperationException("Native barcode result payload length is invalid: " + native.TextLen + ".");
                return 1;
            }

            var bytes = new byte[native.TextLen];

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
        }
        catch (Exception ex)
        {
            state.CallbackException = ex;
            return 1;
        }

        return 0;
    }

    private static void EnsureReadablePath(string path, string paramName)
    {
        if (path == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", paramName);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input file not found.", path);
        }
    }

    private static void EnsureReadableBuffer(byte[] bytes, string paramName)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (bytes.Length == 0)
        {
            throw new ArgumentException("Input buffer must not be empty.", paramName);
        }
    }

    private static void EnsureReadableStream(Stream stream, string paramName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", paramName);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(QualitySoftBarcodeReader));
        }
    }

    private void SafeReleaseNativeScanSlot()
    {
        try
        {
            _nativeScanSlots.Release();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static BarcodeReaderSettings CreateSettings(BarcodeReaderOptions defaultOptions)
    {
        if (defaultOptions == null)
        {
            throw new ArgumentNullException(nameof(defaultOptions));
        }

        return new BarcodeReaderSettings { DefaultOptions = defaultOptions };
    }

    private sealed class ScanCallbackState
    {
        internal ScanCallbackState(Encoding? textEncoding)
        {
            TextEncoding = textEncoding;
        }

        internal List<BarcodeResult> Results { get; } = new List<BarcodeResult>();

        internal Encoding? TextEncoding { get; }

        internal Exception? CallbackException { get; set; }
    }
}
