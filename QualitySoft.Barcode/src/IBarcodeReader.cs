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

    /// <summary>
    /// Detects the input format for a stream. The stream is copied to memory before detection.
    /// </summary>
    BarcodeImageFormat DetectFormat(Stream stream);
}
