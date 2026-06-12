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
    private readonly NativeScanWorkerPool _nativeScanWorkers;
    private readonly bool _copyInputBuffersForAsyncByteArray;
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
        _nativeScanWorkers = new NativeScanWorkerPool(snapshot.MaxConcurrentScans, snapshot.NativeScanThreadStackSize);
        _copyInputBuffersForAsyncByteArray = snapshot.CopyInputBuffersForAsyncByteArray;

        var pdfRenderWorkerWarmupCount = snapshot.GetEffectivePdfRenderWorkerWarmupCount();
        if (pdfRenderWorkerWarmupCount > 0)
        {
            BarcodeNativeLibrary.WarmUpPdfRenderWorkers((uint)pdfRenderWorkerWarmupCount);
        }
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe IReadOnlyList<BarcodeResult> Read(ReadOnlySpan<byte> bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        fixed (byte* pointer = bytes)
        {
            return Read((IntPtr)pointer, bytes.Length, options);
        }
    }

    public unsafe IReadOnlyList<BarcodeResult> Read(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        using (var handle = bytes.Pin())
        {
            return Read((IntPtr)handle.Pointer, bytes.Length, options);
        }
    }
#endif

    public IReadOnlyList<BarcodeResult> Read(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => ReadMemoryCore(bytes, byteLength, effectiveOptions));
    }

    public IReadOnlyList<BarcodeResult> ReadRawGray8(byte[] pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null)
    {
        EnsureRawGray8Buffer(pixels, width, height, stride, nameof(pixels));

        var effectiveOptions = GetOptionsSnapshot(options);
        var pixelsHandle = default(GCHandle);
        try
        {
            pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            return RunNativeScan(() => ReadRawGray8Core(pixelsHandle.AddrOfPinnedObject(), width, height, stride, effectiveOptions));
        }
        finally
        {
            if (pixelsHandle.IsAllocated)
            {
                pixelsHandle.Free();
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe IReadOnlyList<BarcodeResult> ReadRawGray8(ReadOnlySpan<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null)
    {
        EnsureRawGray8Buffer(pixels.Length, width, height, stride, nameof(pixels));

        fixed (byte* pointer = pixels)
        {
            return ReadRawGray8((IntPtr)pointer, width, height, stride, options);
        }
    }

    public unsafe IReadOnlyList<BarcodeResult> ReadRawGray8(ReadOnlyMemory<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null)
    {
        EnsureRawGray8Buffer(pixels.Length, width, height, stride, nameof(pixels));

        using (var handle = pixels.Pin())
        {
            return ReadRawGray8((IntPtr)handle.Pointer, width, height, stride, options);
        }
    }
#endif

    public IReadOnlyList<BarcodeResult> ReadRawGray8(IntPtr pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null)
    {
        EnsureRawGray8Pointer(pixels, width, height, stride, nameof(pixels));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => ReadRawGray8Core(pixels, width, height, stride, effectiveOptions));
    }

    public IReadOnlyList<BarcodeResult> ReadRawPixels(byte[] pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null)
    {
        var requiredLength = EnsureRawPixelBuffer(pixels, width, height, pixelFormat, stride, nameof(pixels));
        if (pixelFormat == BarcodeRawPixelFormat.Gray8)
        {
            return ReadRawGray8(pixels, width, height, stride, options);
        }

        var gray = ConvertRawPixelsToGray8(pixels, requiredLength, width, height, pixelFormat, stride);
        return ReadRawGray8(gray, width, height, width, options);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public IReadOnlyList<BarcodeResult> ReadRawPixels(ReadOnlySpan<byte> pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null)
    {
        var requiredLength = EnsureRawPixelBuffer(pixels.Length, width, height, pixelFormat, stride, nameof(pixels));
        if (pixelFormat == BarcodeRawPixelFormat.Gray8)
        {
            return ReadRawGray8(pixels, width, height, stride, options);
        }

        var source = pixels.Slice(0, requiredLength).ToArray();
        var gray = ConvertRawPixelsToGray8(source, requiredLength, width, height, pixelFormat, stride);
        return ReadRawGray8(gray, width, height, width, options);
    }

    public IReadOnlyList<BarcodeResult> ReadRawPixels(ReadOnlyMemory<byte> pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null)
    {
        var requiredLength = EnsureRawPixelBuffer(pixels.Length, width, height, pixelFormat, stride, nameof(pixels));
        if (pixelFormat == BarcodeRawPixelFormat.Gray8)
        {
            return ReadRawGray8(pixels, width, height, stride, options);
        }

        var source = pixels.Slice(0, requiredLength).ToArray();
        var gray = ConvertRawPixelsToGray8(source, requiredLength, width, height, pixelFormat, stride);
        return ReadRawGray8(gray, width, height, width, options);
    }
#endif

    public IReadOnlyList<BarcodeResult> ReadRawPixels(IntPtr pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null)
    {
        EnsureRawPixelPointer(pixels, width, height, pixelFormat, stride, nameof(pixels));
        if (pixelFormat == BarcodeRawPixelFormat.Gray8)
        {
            return ReadRawGray8(pixels, width, height, stride, options);
        }

        var requiredLength = GetRequiredRawPixelLength(width, height, pixelFormat, stride);
        var source = new byte[requiredLength];
        Marshal.Copy(pixels, source, 0, source.Length);
        var gray = ConvertRawPixelsToGray8(source, requiredLength, width, height, pixelFormat, stride);
        return ReadRawGray8(gray, width, height, width, options);
    }

    private IReadOnlyList<BarcodeResult> ReadBytesCore(byte[] bytes, BarcodeReaderOptions effectiveOptions)
    {
        var bytesHandle = default(GCHandle);
        try
        {
            bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return ReadMemoryCore(bytesHandle.AddrOfPinnedObject(), bytes.Length, effectiveOptions);
        }
        finally
        {
            if (bytesHandle.IsAllocated)
            {
                bytesHandle.Free();
            }
        }
    }

    private IReadOnlyList<BarcodeResult> ReadMemoryCore(IntPtr bytes, int byteLength, BarcodeReaderOptions effectiveOptions)
    {
        var nativeOptions = ToNativeOptions(effectiveOptions);
        return ScanWithHandle(
            results => NativeMethods.qsbc_loader_scan_image_memory_cb_with_options(
                bytes,
                (UIntPtr)byteLength,
                ref nativeOptions,
                CollectCallback,
                results),
            effectiveOptions.TextEncoding,
            effectiveOptions.Symbologies);
    }

    private IReadOnlyList<BarcodeResult> ReadRawGray8Core(IntPtr pixels, int width, int height, int stride, BarcodeReaderOptions effectiveOptions)
    {
        var nativeOptions = ToNativeOptions(effectiveOptions);
        var effectiveStride = GetEffectiveRawGray8Stride(width, stride);
        return ScanWithHandle(
            results => NativeMethods.qsbc_loader_scan_gray8_cb_with_options(
                pixels,
                checked((uint)width),
                checked((uint)height),
                checked((uint)effectiveStride),
                ref nativeOptions,
                CollectCallback,
                results),
            effectiveOptions.TextEncoding,
            effectiveOptions.Symbologies);
    }

    private IReadOnlyList<BarcodeResult> ReadMemoryStreamBuffer(MemoryStream memory, BarcodeReaderOptions effectiveOptions)
    {
        return RunNativeScan(() => ReadMemoryStreamBufferCore(memory, effectiveOptions));
    }

    private IReadOnlyList<BarcodeResult> ReadMemoryStreamBufferCore(MemoryStream memory, BarcodeReaderOptions effectiveOptions)
    {
        if (!memory.TryGetBuffer(out var segment) || segment.Array == null)
        {
            return ReadBytesCore(memory.ToArray(), effectiveOptions);
        }

        var handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            return ReadMemoryCore(IntPtr.Add(handle.AddrOfPinnedObject(), segment.Offset), segment.Count, effectiveOptions);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    public IReadOnlyList<BarcodeResult> Read(Stream stream, BarcodeReaderOptions? options = null)
    {
        EnsureReadableStream(stream, nameof(stream));
        var effectiveOptions = GetOptionsSnapshot(options);

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory, StreamCopyBufferSize);
            return ReadMemoryStreamBuffer(memory, effectiveOptions);
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
        var input = bytes;
        if (_copyInputBuffersForAsyncByteArray)
        {
            input = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, input, 0, bytes.Length);
        }

        return RunNativeScanAsync(() => ReadBytesCore(input, effectiveOptions), cancellationToken);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));
        var effectiveOptions = GetOptionsSnapshot(options);

        return RunNativeScanAsync(() =>
        {
            unsafe
            {
                using (var handle = bytes.Pin())
                {
                    return ReadMemoryCore((IntPtr)handle.Pointer, bytes.Length, effectiveOptions);
                }
            }
        }, cancellationToken);
    }
#endif

    public Task<IReadOnlyList<BarcodeResult>> ReadAsync(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));
        var effectiveOptions = GetOptionsSnapshot(options);

        return RunNativeScanAsync(() => ReadMemoryCore(bytes, byteLength, effectiveOptions), cancellationToken);
    }

    public Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(byte[] pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requiredLength = EnsureRawGray8Buffer(pixels, width, height, stride, nameof(pixels));
        var effectiveOptions = GetOptionsSnapshot(options);
        var pixelsSnapshot = new byte[requiredLength];
        Buffer.BlockCopy(pixels, 0, pixelsSnapshot, 0, pixelsSnapshot.Length);

        return RunNativeScanAsync(() => ReadRawGray8(pixelsSnapshot, width, height, stride, effectiveOptions), cancellationToken);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(ReadOnlyMemory<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureRawGray8Buffer(pixels.Length, width, height, stride, nameof(pixels));
        var effectiveOptions = GetOptionsSnapshot(options);

        return RunNativeScanAsync(() =>
        {
            unsafe
            {
                using (var handle = pixels.Pin())
                {
                    return ReadRawGray8Core((IntPtr)handle.Pointer, width, height, stride, effectiveOptions);
                }
            }
        }, cancellationToken);
    }
#endif

    public Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(IntPtr pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureRawGray8Pointer(pixels, width, height, stride, nameof(pixels));
        var effectiveOptions = GetOptionsSnapshot(options);

        return RunNativeScanAsync(() => ReadRawGray8Core(pixels, width, height, stride, effectiveOptions), cancellationToken);
    }

    public async Task<IReadOnlyList<BarcodeResult>> ReadAsync(Stream stream, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadableStream(stream, nameof(stream));
        var effectiveOptions = GetOptionsSnapshot(options);

        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, StreamCopyBufferSize, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await RunNativeScanAsync(() => ReadMemoryStreamBufferCore(memory, effectiveOptions), cancellationToken).ConfigureAwait(false);
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

        return DetectFormatBytes(bytes);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe BarcodeImageFormat DetectFormat(ReadOnlySpan<byte> bytes)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        fixed (byte* pointer = bytes)
        {
            return DetectFormat((IntPtr)pointer, bytes.Length);
        }
    }

    public unsafe BarcodeImageFormat DetectFormat(ReadOnlyMemory<byte> bytes)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        using (var handle = bytes.Pin())
        {
            return DetectFormat((IntPtr)handle.Pointer, bytes.Length);
        }
    }
#endif

    public BarcodeImageFormat DetectFormat(IntPtr bytes, int byteLength)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));

        return (BarcodeImageFormat)NativeMethods.Invoke(() => NativeMethods.qsbc_loader_detect_image_format(bytes, (UIntPtr)byteLength));
    }

    public BarcodeImageFormat DetectFormat(Stream stream)
    {
        EnsureReadableStream(stream, nameof(stream));

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory, StreamCopyBufferSize);
            return DetectFormatMemoryStreamBuffer(memory);
        }
    }

    private static BarcodeImageFormat DetectFormatMemoryStreamBuffer(MemoryStream memory)
    {
        if (!memory.TryGetBuffer(out var segment) || segment.Array == null)
        {
            return DetectFormatBytes(memory.ToArray());
        }

        var handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            return (BarcodeImageFormat)NativeMethods.Invoke(() => NativeMethods.qsbc_loader_detect_image_format(
                IntPtr.Add(handle.AddrOfPinnedObject(), segment.Offset),
                (UIntPtr)segment.Count));
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    private static BarcodeImageFormat DetectFormatBytes(byte[] bytes)
    {
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

    public int GetPageCount(string path)
    {
        EnsureReadablePath(path, nameof(path));

        var count = NativeMethods.Invoke(() => NativeMethods.qsbc_loader_page_count_file(NativeMethods.ToNullTerminatedUtf8(path)));
        return EnsurePositivePageCount(count);
    }

    public int GetPageCount(FileInfo file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return GetPageCount(file.FullName);
    }

    public int GetPageCount(byte[] bytes)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        var handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return GetPageCount(handle.AddrOfPinnedObject(), bytes.Length);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe int GetPageCount(ReadOnlySpan<byte> bytes)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        fixed (byte* pointer = bytes)
        {
            return GetPageCount((IntPtr)pointer, bytes.Length);
        }
    }

    public unsafe int GetPageCount(ReadOnlyMemory<byte> bytes)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        using (var handle = bytes.Pin())
        {
            return GetPageCount((IntPtr)handle.Pointer, bytes.Length);
        }
    }
#endif

    public int GetPageCount(IntPtr bytes, int byteLength)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));

        var count = NativeMethods.Invoke(() => NativeMethods.qsbc_loader_page_count_image_memory(bytes, (UIntPtr)byteLength));
        return EnsurePositivePageCount(count);
    }

    public int GetPageCount(Stream stream)
    {
        EnsureReadableStream(stream, nameof(stream));

        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory, StreamCopyBufferSize);
            if (!memory.TryGetBuffer(out var segment) || segment.Array == null)
            {
                return GetPageCount(memory.ToArray());
            }

            var handle = default(GCHandle);
            try
            {
                handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
                return GetPageCount(IntPtr.Add(handle.AddrOfPinnedObject(), segment.Offset), segment.Count);
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
    }

    public BarcodeRenderedImage RenderPage(string path, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePath(path, nameof(path));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderFilePageCore(path, effectiveOptions, BarcodeRenderedPixelFormat.Bmp24));
    }

    public BarcodeRenderedImage RenderPage(FileInfo file, BarcodeReaderOptions? options = null)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return RenderPage(file.FullName, options);
    }

    public BarcodeRenderedImage RenderPage(byte[] bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderBytesCore(bytes, effectiveOptions, BarcodeRenderedPixelFormat.Bmp24));
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe BarcodeRenderedImage RenderPage(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() =>
        {
            using (var handle = bytes.Pin())
            {
                return RenderMemoryPageCore((IntPtr)handle.Pointer, bytes.Length, effectiveOptions, BarcodeRenderedPixelFormat.Bmp24);
            }
        });
    }
#endif

    public BarcodeRenderedImage RenderPage(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderMemoryPageCore(bytes, byteLength, effectiveOptions, BarcodeRenderedPixelFormat.Bmp24));
    }

    public BarcodeRenderedImage RenderPageGray8(string path, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePath(path, nameof(path));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderFilePageCore(path, effectiveOptions, BarcodeRenderedPixelFormat.Gray8));
    }

    public BarcodeRenderedImage RenderPageGray8(FileInfo file, BarcodeReaderOptions? options = null)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return RenderPageGray8(file.FullName, options);
    }

    public BarcodeRenderedImage RenderPageGray8(byte[] bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderBytesCore(bytes, effectiveOptions, BarcodeRenderedPixelFormat.Gray8));
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    public unsafe BarcodeRenderedImage RenderPageGray8(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null)
    {
        EnsureReadableBuffer(bytes, nameof(bytes));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() =>
        {
            using (var handle = bytes.Pin())
            {
                return RenderMemoryPageCore((IntPtr)handle.Pointer, bytes.Length, effectiveOptions, BarcodeRenderedPixelFormat.Gray8);
            }
        });
    }
#endif

    public BarcodeRenderedImage RenderPageGray8(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null)
    {
        EnsureReadablePointer(bytes, byteLength, nameof(bytes), nameof(byteLength));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScan(() => RenderMemoryPageCore(bytes, byteLength, effectiveOptions, BarcodeRenderedPixelFormat.Gray8));
    }

    public IReadOnlyList<BarcodeRenderedImage> RenderPages(string path, BarcodeReaderOptions? options = null)
    {
        return RenderPagesCore(path, options, BarcodeRenderedPixelFormat.Bmp24);
    }

    public IReadOnlyList<BarcodeRenderedImage> RenderPagesGray8(string path, BarcodeReaderOptions? options = null)
    {
        return RenderPagesCore(path, options, BarcodeRenderedPixelFormat.Gray8);
    }

    public Task<BarcodeRenderedImage> RenderPageAsync(string path, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReadablePath(path, nameof(path));

        var effectiveOptions = GetOptionsSnapshot(options);
        return RunNativeScanAsync(() => RenderFilePageCore(path, effectiveOptions, BarcodeRenderedPixelFormat.Bmp24), cancellationToken);
    }

    public Task<BarcodeRenderedImage> RenderPageAsync(FileInfo file, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return RenderPageAsync(file.FullName, options, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _nativeScanWorkers.Dispose();
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

    private static BarcodeRenderedImage RenderFilePageCore(string path, BarcodeReaderOptions effectiveOptions, BarcodeRenderedPixelFormat pixelFormat)
    {
        var nativeOptions = ToNativeOptions(effectiveOptions);
        var output = new NativeImageBuffer();
        try
        {
            var pathBytes = NativeMethods.ToNullTerminatedUtf8(path);
            var status = pixelFormat == BarcodeRenderedPixelFormat.Gray8
                ? NativeMethods.Invoke(() => NativeMethods.qsbc_loader_render_file_page_gray8_with_options(pathBytes, ref nativeOptions, ref output))
                : NativeMethods.Invoke(() => NativeMethods.qsbc_loader_render_file_page_bmp_with_options(pathBytes, ref nativeOptions, ref output));

            return CreateRenderedImage(status, ref output, pixelFormat);
        }
        finally
        {
            NativeMethods.Invoke(() => NativeMethods.qsbc_loader_free_image_buffer(ref output));
        }
    }

    private static BarcodeRenderedImage RenderBytesCore(byte[] bytes, BarcodeReaderOptions effectiveOptions, BarcodeRenderedPixelFormat pixelFormat)
    {
        var handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return RenderMemoryPageCore(handle.AddrOfPinnedObject(), bytes.Length, effectiveOptions, pixelFormat);
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    private static BarcodeRenderedImage RenderMemoryPageCore(IntPtr bytes, int byteLength, BarcodeReaderOptions effectiveOptions, BarcodeRenderedPixelFormat pixelFormat)
    {
        var nativeOptions = ToNativeOptions(effectiveOptions);
        var output = new NativeImageBuffer();
        try
        {
            var status = pixelFormat == BarcodeRenderedPixelFormat.Gray8
                ? NativeMethods.Invoke(() => NativeMethods.qsbc_loader_render_image_memory_page_gray8_with_options(bytes, (UIntPtr)byteLength, ref nativeOptions, ref output))
                : NativeMethods.Invoke(() => NativeMethods.qsbc_loader_render_image_memory_page_bmp_with_options(bytes, (UIntPtr)byteLength, ref nativeOptions, ref output));

            return CreateRenderedImage(status, ref output, pixelFormat);
        }
        finally
        {
            NativeMethods.Invoke(() => NativeMethods.qsbc_loader_free_image_buffer(ref output));
        }
    }

    private IReadOnlyList<BarcodeRenderedImage> RenderPagesCore(string path, BarcodeReaderOptions? options, BarcodeRenderedPixelFormat pixelFormat)
    {
        EnsureReadablePath(path, nameof(path));

        var totalPageCount = GetPageCount(path);
        var baseOptions = GetOptionsSnapshot(options);
        var start = baseOptions.PageStart < 0 ? 0 : baseOptions.PageStart;
        if (start >= totalPageCount)
        {
            return Array.Empty<BarcodeRenderedImage>();
        }

        var requestedCount = baseOptions.PageCount <= 0 ? totalPageCount - start : Math.Min(baseOptions.PageCount, totalPageCount - start);
        var images = new List<BarcodeRenderedImage>(requestedCount);
        for (var page = start; page < start + requestedCount; page++)
        {
            var pageOptions = baseOptions.Clone();
            pageOptions.PageStart = page;
            pageOptions.PageCount = 1;
            images.Add(RunNativeScan(() => RenderFilePageCore(path, pageOptions, pixelFormat)));
        }

        return images;
    }

    private static BarcodeRenderedImage CreateRenderedImage(int status, ref NativeImageBuffer output, BarcodeRenderedPixelFormat pixelFormat)
    {
        if (status < 0)
        {
            throw new BarcodeScanException(status, NativeMethods.PtrToString(NativeMethods.qsbc_loader_status_name(status)));
        }

        if (output.Data == IntPtr.Zero || output.Len == UIntPtr.Zero)
        {
            throw new InvalidOperationException("Native page rendering did not return image data.");
        }

        var byteLength = checked((int)output.Len.ToUInt64());
        var bytes = new byte[byteLength];
        Marshal.Copy(output.Data, bytes, 0, bytes.Length);

        var stride = pixelFormat == BarcodeRenderedPixelFormat.Gray8 ? output.Width : 0;
        return new BarcodeRenderedImage(
            bytes,
            output.Width,
            output.Height,
            (BarcodeImageFormat)output.Format,
            output.PageIndex,
            pixelFormat,
            stride);
    }

    private static int EnsurePositivePageCount(int count)
    {
        if (count < 0)
        {
            throw new BarcodeScanException(count, NativeMethods.PtrToString(NativeMethods.qsbc_loader_status_name(count)));
        }

        if (count == 0)
        {
            throw new InvalidOperationException("Native page count returned zero.");
        }

        return count;
    }

    private IReadOnlyList<BarcodeResult> RunNativeScan(Func<IReadOnlyList<BarcodeResult>> scan)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        return RunNativeScan<IReadOnlyList<BarcodeResult>>(scan);
    }

    private T RunNativeScan<T>(Func<T> scan)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        return _nativeScanWorkers.Invoke(() =>
        {
            ThrowIfDisposed();
            return scan();
        });
    }

    private async Task<IReadOnlyList<BarcodeResult>> RunNativeScanAsync(Func<IReadOnlyList<BarcodeResult>> scan, CancellationToken cancellationToken)
    {
        return await RunNativeScanAsync<IReadOnlyList<BarcodeResult>>(scan, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> RunNativeScanAsync<T>(Func<T> scan, CancellationToken cancellationToken)
    {
        if (scan == null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        return await _nativeScanWorkers.InvokeAsync(() =>
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            return scan();
        }, cancellationToken).ConfigureAwait(false);
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    private static void EnsureReadableBuffer(ReadOnlySpan<byte> bytes, string paramName)
    {
        if (bytes.Length == 0)
        {
            throw new ArgumentException("Input buffer must not be empty.", paramName);
        }
    }

    private static void EnsureReadableBuffer(ReadOnlyMemory<byte> bytes, string paramName)
    {
        if (bytes.Length == 0)
        {
            throw new ArgumentException("Input buffer must not be empty.", paramName);
        }
    }
#endif

    private static void EnsureReadablePointer(IntPtr bytes, int byteLength, string pointerParamName, string lengthParamName)
    {
        if (bytes == IntPtr.Zero)
        {
            throw new ArgumentNullException(pointerParamName);
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(lengthParamName, byteLength, "Input buffer length must be greater than zero.");
        }
    }

    private static int EnsureRawGray8Buffer(byte[] pixels, int width, int height, int stride, string paramName)
    {
        if (pixels == null)
        {
            throw new ArgumentNullException(paramName);
        }

        return EnsureRawGray8Buffer(pixels.Length, width, height, stride, paramName);
    }

    private static int EnsureRawGray8Buffer(int bufferLength, int width, int height, int stride, string paramName)
    {
        var requiredLength = GetRequiredRawGray8ByteLength(width, height, stride);
        if (bufferLength < requiredLength)
        {
            throw new ArgumentException("Raw Gray8 input buffer is smaller than width, height and stride require.", paramName);
        }

        return requiredLength;
    }

    private static void EnsureRawGray8Pointer(IntPtr pixels, int width, int height, int stride, string pointerParamName)
    {
        if (pixels == IntPtr.Zero)
        {
            throw new ArgumentNullException(pointerParamName);
        }

        GetRequiredRawGray8ByteLength(width, height, stride);
    }

    private static int EnsureRawPixelBuffer(byte[] pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride, string paramName)
    {
        if (pixels == null)
        {
            throw new ArgumentNullException(paramName);
        }

        return EnsureRawPixelBuffer(pixels.Length, width, height, pixelFormat, stride, paramName);
    }

    private static int EnsureRawPixelBuffer(int bufferLength, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride, string paramName)
    {
        var requiredLength = GetRequiredRawPixelLength(width, height, pixelFormat, stride);
        if (bufferLength < requiredLength)
        {
            throw new ArgumentException("Raw pixel input buffer is smaller than width, height, format and stride require.", paramName);
        }

        return requiredLength;
    }

    private static void EnsureRawPixelPointer(IntPtr pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride, string pointerParamName)
    {
        if (pixels == IntPtr.Zero)
        {
            throw new ArgumentNullException(pointerParamName);
        }

        GetRequiredRawPixelLength(width, height, pixelFormat, stride);
    }

    private static int GetRequiredRawPixelLength(int width, int height, BarcodeRawPixelFormat pixelFormat, int stride)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Raw pixel width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Raw pixel height must be greater than zero.");
        }

        var bytesPerPixel = GetRawPixelBytesPerPixel(pixelFormat);
        if (stride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "Raw pixel stride must be zero or greater.");
        }

        var minimumStride = checked(width * bytesPerPixel);
        var effectiveStride = stride == 0 ? minimumStride : stride;
        if (effectiveStride < minimumStride)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "Raw pixel stride must be zero or at least width multiplied by bytes per pixel.");
        }

        var requiredLength = ((long)(height - 1) * effectiveStride) + minimumStride;
        if (requiredLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Raw pixel buffer is too large for the managed wrapper.");
        }

        return (int)requiredLength;
    }

    private static int GetRawPixelBytesPerPixel(BarcodeRawPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case BarcodeRawPixelFormat.Gray8:
                return 1;
            case BarcodeRawPixelFormat.Rgb24:
            case BarcodeRawPixelFormat.Bgr24:
                return 3;
            case BarcodeRawPixelFormat.Rgba32:
            case BarcodeRawPixelFormat.Bgra32:
                return 4;
            default:
                throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, "Unsupported raw pixel format.");
        }
    }

    private static byte[] ConvertRawPixelsToGray8(byte[] pixels, int requiredLength, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride)
    {
        var bytesPerPixel = GetRawPixelBytesPerPixel(pixelFormat);
        var sourceStride = stride == 0 ? width * bytesPerPixel : stride;
        var gray = new byte[checked(width * height)];

        for (var y = 0; y < height; y++)
        {
            var sourceRow = y * sourceStride;
            var targetRow = y * width;
            for (var x = 0; x < width; x++)
            {
                var source = sourceRow + x * bytesPerPixel;
                if (source + bytesPerPixel > requiredLength)
                {
                    throw new ArgumentException("Raw pixel input buffer is smaller than width, height, format and stride require.", nameof(pixels));
                }

                byte r;
                byte g;
                byte b;
                switch (pixelFormat)
                {
                    case BarcodeRawPixelFormat.Rgb24:
                    case BarcodeRawPixelFormat.Rgba32:
                        r = pixels[source];
                        g = pixels[source + 1];
                        b = pixels[source + 2];
                        break;
                    case BarcodeRawPixelFormat.Bgr24:
                    case BarcodeRawPixelFormat.Bgra32:
                        b = pixels[source];
                        g = pixels[source + 1];
                        r = pixels[source + 2];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, "Unsupported raw pixel format.");
                }

                gray[targetRow + x] = (byte)((r * 299 + g * 587 + b * 114 + 500) / 1000);
            }
        }

        return gray;
    }

    private static int GetRequiredRawGray8ByteLength(int width, int height, int stride)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Raw Gray8 width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Raw Gray8 height must be greater than zero.");
        }

        var effectiveStride = GetEffectiveRawGray8Stride(width, stride);
        var requiredLength = ((long)(height - 1) * effectiveStride) + width;
        if (requiredLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Raw Gray8 buffer is too large for the managed wrapper.");
        }

        return (int)requiredLength;
    }

    private static int GetEffectiveRawGray8Stride(int width, int stride)
    {
        if (stride < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "Raw Gray8 stride must be zero or greater.");
        }

        var effectiveStride = stride == 0 ? width : stride;
        if (effectiveStride < width)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "Raw Gray8 stride must be zero or at least the image width.");
        }

        return effectiveStride;
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
