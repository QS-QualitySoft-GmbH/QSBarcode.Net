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

    internal BarcodeReaderSettings Clone()
    {
        return new BarcodeReaderSettings
        {
            DefaultOptions = DefaultOptions.Clone(),
            MaxConcurrentScans = MaxConcurrentScans,
            NativeScanThreadStackSize = NativeScanThreadStackSize
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
    }
}
