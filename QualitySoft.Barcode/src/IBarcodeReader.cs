using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QualitySoft.Barcode;

/// <summary>
/// Reads barcodes from files, byte arrays and streams using the native QS Barcode SDK.
/// </summary>
public interface IBarcodeReader
{
    /// <summary>
    /// Reads barcodes from a file path.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(string path, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a file.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(FileInfo file, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from an encoded image or document in memory.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(byte[] bytes, BarcodeReaderOptions? options = null);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Reads barcodes from an encoded image or document in memory.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(ReadOnlySpan<byte> bytes, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from an encoded image or document in memory.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null);
#endif

    /// <summary>
    /// Reads barcodes from an encoded image or document at an unmanaged memory address.
    /// The caller must keep the memory valid and unchanged for the duration of the call.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer.
    /// Stride zero means tightly packed rows with one byte per pixel.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawGray8(byte[] pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer.
    /// Stride zero means tightly packed rows with one byte per pixel.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawGray8(ReadOnlySpan<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer.
    /// Stride zero means tightly packed rows with one byte per pixel.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawGray8(ReadOnlyMemory<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null);
#endif

    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer at an unmanaged memory address.
    /// Stride zero means tightly packed rows with one byte per pixel.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawGray8(IntPtr pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a raw pixel buffer. Non-Gray8 formats are converted to Gray8 before scanning.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawPixels(byte[] pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Reads barcodes from a raw pixel buffer. Non-Gray8 formats are converted to Gray8 before scanning.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawPixels(ReadOnlySpan<byte> pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a raw pixel buffer. Non-Gray8 formats are converted to Gray8 before scanning.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawPixels(ReadOnlyMemory<byte> pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null);
#endif

    /// <summary>
    /// Reads barcodes from a raw pixel buffer at an unmanaged memory address. Non-Gray8 formats are copied and converted to Gray8 before scanning.
    /// </summary>
    IReadOnlyList<BarcodeResult> ReadRawPixels(IntPtr pixels, int width, int height, BarcodeRawPixelFormat pixelFormat, int stride = 0, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a stream. The stream is copied to memory before the native scan starts.
    /// </summary>
    IReadOnlyList<BarcodeResult> Read(Stream stream, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Reads barcodes from a file path on a dedicated native scan thread.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(string path, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads barcodes from a file on a dedicated native scan thread.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(FileInfo file, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads barcodes from an encoded image or document in memory on a dedicated native scan thread.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(byte[] bytes, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Reads barcodes from an encoded image or document in memory on a dedicated native scan thread.
    /// The caller must keep the memory valid and unchanged until the returned task completes.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);
#endif

    /// <summary>
    /// Reads barcodes from an encoded image or document at an unmanaged memory address on a dedicated native scan thread.
    /// The caller must keep the memory valid and unchanged until the returned task completes.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer on a dedicated native scan thread.
    /// Stride zero means tightly packed rows with one byte per pixel.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(byte[] pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer on a dedicated native scan thread.
    /// The caller must keep the memory valid and unchanged until the returned task completes.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(ReadOnlyMemory<byte> pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);
#endif

    /// <summary>
    /// Reads barcodes from a raw 8-bit grayscale image buffer at an unmanaged memory address on a dedicated native scan thread.
    /// The caller must keep the memory valid and unchanged until the returned task completes.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadRawGray8Async(IntPtr pixels, int width, int height, int stride = 0, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads barcodes from a stream asynchronously. The stream copy observes cancellation; an already running native scan cannot be interrupted.
    /// </summary>
    Task<IReadOnlyList<BarcodeResult>> ReadAsync(Stream stream, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the input format for a file path.
    /// </summary>
    BarcodeImageFormat DetectFormat(string path);

    /// <summary>
    /// Detects the input format for a file.
    /// </summary>
    BarcodeImageFormat DetectFormat(FileInfo file);

    /// <summary>
    /// Detects the input format for an encoded image or document in memory.
    /// </summary>
    BarcodeImageFormat DetectFormat(byte[] bytes);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Detects the input format for an encoded image or document in memory.
    /// </summary>
    BarcodeImageFormat DetectFormat(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Detects the input format for an encoded image or document in memory.
    /// </summary>
    BarcodeImageFormat DetectFormat(ReadOnlyMemory<byte> bytes);
#endif

    /// <summary>
    /// Detects the input format for an encoded image or document at an unmanaged memory address.
    /// The caller must keep the memory valid and unchanged for the duration of the call.
    /// </summary>
    BarcodeImageFormat DetectFormat(IntPtr bytes, int byteLength);

    /// <summary>
    /// Detects the input format for a stream. The stream is copied to memory before detection.
    /// </summary>
    BarcodeImageFormat DetectFormat(Stream stream);

    /// <summary>
    /// Returns the page or frame count for a file. Single-image inputs return one.
    /// </summary>
    int GetPageCount(string path);

    /// <summary>
    /// Returns the page or frame count for a file. Single-image inputs return one.
    /// </summary>
    int GetPageCount(FileInfo file);

    /// <summary>
    /// Returns the page or frame count for encoded image or document bytes. Single-image inputs return one.
    /// </summary>
    int GetPageCount(byte[] bytes);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Returns the page or frame count for encoded image or document bytes. Single-image inputs return one.
    /// </summary>
    int GetPageCount(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// Returns the page or frame count for encoded image or document bytes. Single-image inputs return one.
    /// </summary>
    int GetPageCount(ReadOnlyMemory<byte> bytes);
#endif

    /// <summary>
    /// Returns the page or frame count for encoded image or document bytes at an unmanaged memory address. Single-image inputs return one.
    /// </summary>
    int GetPageCount(IntPtr bytes, int byteLength);

    /// <summary>
    /// Returns the page or frame count for encoded image or document stream content. Single-image inputs return one.
    /// </summary>
    int GetPageCount(Stream stream);

    /// <summary>
    /// Renders a file page or frame through the native loader and returns 24-bit BMP bytes.
    /// PDF input uses the native PDF render worker process when available.
    /// </summary>
    BarcodeRenderedImage RenderPage(string path, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders a file page or frame through the native loader and returns 24-bit BMP bytes.
    /// PDF input uses the native PDF render worker process when available.
    /// </summary>
    BarcodeRenderedImage RenderPage(FileInfo file, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders encoded image or document bytes and returns 24-bit BMP bytes.
    /// </summary>
    BarcodeRenderedImage RenderPage(byte[] bytes, BarcodeReaderOptions? options = null);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Renders encoded image or document bytes and returns 24-bit BMP bytes.
    /// </summary>
    BarcodeRenderedImage RenderPage(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null);
#endif

    /// <summary>
    /// Renders encoded image or document bytes at an unmanaged memory address and returns 24-bit BMP bytes.
    /// </summary>
    BarcodeRenderedImage RenderPage(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders a file page or frame and returns raw Gray8 pixels.
    /// </summary>
    BarcodeRenderedImage RenderPageGray8(string path, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders a file page or frame and returns raw Gray8 pixels.
    /// </summary>
    BarcodeRenderedImage RenderPageGray8(FileInfo file, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders encoded image or document bytes and returns raw Gray8 pixels.
    /// </summary>
    BarcodeRenderedImage RenderPageGray8(byte[] bytes, BarcodeReaderOptions? options = null);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Renders encoded image or document bytes and returns raw Gray8 pixels.
    /// </summary>
    BarcodeRenderedImage RenderPageGray8(ReadOnlyMemory<byte> bytes, BarcodeReaderOptions? options = null);
#endif

    /// <summary>
    /// Renders encoded image or document bytes at an unmanaged memory address and returns raw Gray8 pixels.
    /// </summary>
    BarcodeRenderedImage RenderPageGray8(IntPtr bytes, int byteLength, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders all selected pages or frames from a file and returns 24-bit BMP bytes.
    /// </summary>
    IReadOnlyList<BarcodeRenderedImage> RenderPages(string path, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders all selected pages or frames from a file and returns raw Gray8 pixels.
    /// </summary>
    IReadOnlyList<BarcodeRenderedImage> RenderPagesGray8(string path, BarcodeReaderOptions? options = null);

    /// <summary>
    /// Renders a file page or frame through the native loader on a dedicated native scan thread.
    /// PDF input uses the native PDF render worker process when available.
    /// </summary>
    Task<BarcodeRenderedImage> RenderPageAsync(string path, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a file page or frame through the native loader on a dedicated native scan thread.
    /// PDF input uses the native PDF render worker process when available.
    /// </summary>
    Task<BarcodeRenderedImage> RenderPageAsync(FileInfo file, BarcodeReaderOptions? options = null, CancellationToken cancellationToken = default);
}
