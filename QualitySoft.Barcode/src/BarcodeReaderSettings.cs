using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Process-level reader settings for concurrency and managed defaults.
/// </summary>
public sealed class BarcodeReaderSettings
{
    /// <summary>
    /// Default stack size used for dedicated native scan threads.
    /// </summary>
    public const int DefaultNativeScanThreadStackSize = 16 * 1024 * 1024;

    /// <summary>
    /// Automatically derives PDF render worker warmup count from <see cref="MaxConcurrentScans"/>.
    /// </summary>
    public const int AutoPdfRenderWorkerWarmupCount = -1;

    /// <summary>
    /// Maximum number of native PDF render worker processes supported by the native loader pool.
    /// </summary>
    public const int MaxPdfRenderWorkerWarmupCount = 4;

    /// <summary>
    /// Managed scan defaults used whenever a read call does not pass options explicitly.
    /// </summary>
    public BarcodeReaderOptions DefaultOptions { get; set; } = new BarcodeReaderOptions();

    /// <summary>
    /// Maximum number of native scans allowed to execute concurrently per reader instance.
    /// Defaults to the processor count, with a minimum of one.
    /// </summary>
    public int MaxConcurrentScans { get; set; } = Math.Max(1, Environment.ProcessorCount);

    /// <summary>
    /// Stack size for dedicated native scan threads. Keep this large enough for the native legacy scanner.
    /// </summary>
    public int NativeScanThreadStackSize { get; set; } = DefaultNativeScanThreadStackSize;

    /// <summary>
    /// Copies byte arrays passed to <c>ReadAsync(byte[])</c> before queuing native work.
    /// Keep enabled for defensive API behavior. Disable for high-throughput callers that keep buffers immutable until the task completes.
    /// </summary>
    public bool CopyInputBuffersForAsyncByteArray { get; set; } = true;

    /// <summary>
    /// Number of native PDF render worker processes to start when the reader is created.
    /// The default derives from <see cref="MaxConcurrentScans"/>. Set zero to keep lazy startup behavior.
    /// </summary>
    public int PdfRenderWorkerWarmupCount { get; set; } = AutoPdfRenderWorkerWarmupCount;

    /// <summary>
    /// Returns the effective PDF render worker warmup count after applying automatic defaults.
    /// </summary>
    public int GetEffectivePdfRenderWorkerWarmupCount()
    {
        return PdfRenderWorkerWarmupCount == AutoPdfRenderWorkerWarmupCount
            ? Math.Min(MaxConcurrentScans, MaxPdfRenderWorkerWarmupCount)
            : PdfRenderWorkerWarmupCount;
    }

    internal BarcodeReaderSettings Clone()
    {
        return new BarcodeReaderSettings
        {
            DefaultOptions = DefaultOptions.Clone(),
            MaxConcurrentScans = MaxConcurrentScans,
            NativeScanThreadStackSize = NativeScanThreadStackSize,
            CopyInputBuffersForAsyncByteArray = CopyInputBuffersForAsyncByteArray,
            PdfRenderWorkerWarmupCount = PdfRenderWorkerWarmupCount
        };
    }

    internal void Validate()
    {
        if (DefaultOptions == null)
        {
            throw new ArgumentNullException(nameof(DefaultOptions));
        }

        DefaultOptions.Validate();

        if (MaxConcurrentScans < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxConcurrentScans), MaxConcurrentScans, "MaxConcurrentScans must be at least one.");
        }

        if (NativeScanThreadStackSize < 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(NativeScanThreadStackSize), NativeScanThreadStackSize, "NativeScanThreadStackSize must be at least 1 MB.");
        }

        if (PdfRenderWorkerWarmupCount < AutoPdfRenderWorkerWarmupCount || PdfRenderWorkerWarmupCount > MaxPdfRenderWorkerWarmupCount)
        {
            throw new ArgumentOutOfRangeException(nameof(PdfRenderWorkerWarmupCount), PdfRenderWorkerWarmupCount, "PdfRenderWorkerWarmupCount must be -1 for automatic behavior or between zero and four.");
        }
    }
}
