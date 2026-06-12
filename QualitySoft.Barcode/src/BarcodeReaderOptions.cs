using System.Runtime.InteropServices;
using System.Text;

namespace QualitySoft.Barcode;

/// <summary>
/// Options for native barcode scanning. Unset numeric values are passed as zero so the native SDK can apply its own defaults.
/// </summary>
public sealed class BarcodeReaderOptions
{
    static BarcodeReaderOptions()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Symbologies to scan. The default value lets the native SDK choose its built-in default mask.
    /// </summary>
    public BarcodeSymbology Symbologies { get; set; } = BarcodeSymbology.NativeDefault;

    /// <summary>
    /// Minimum decoded text length. Values below 1 are normalized to 1 before calling the native SDK.
    /// </summary>
    public uint MinLength { get; set; } = 1;

    /// <summary>
    /// Optional engine-specific scan flags.
    /// </summary>
    public BarcodeScanFlags Flags { get; set; }

    /// <summary>
    /// Zero-based first page for PDF/TIFF inputs. The default value scans from the first page.
    /// </summary>
    public int PageStart { get; set; } = -1;

    /// <summary>
    /// Number of pages to scan for multi-page inputs. Zero means all pages from <see cref="PageStart"/>.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Render DPI for PDF inputs. Zero lets the native SDK choose its default.
    /// </summary>
    public uint Dpi { get; set; }

    /// <summary>
    /// DataMatrix finder angle tolerance. Zero lets the native SDK choose its default.
    /// </summary>
    public uint DataMatrixFinderAngleTolerance { get; set; }

    /// <summary>
    /// DataMatrix overlap percentage. Zero lets the native SDK choose its default.
    /// </summary>
    public uint DataMatrixOverlapPercent { get; set; }

    /// <summary>
    /// DataMatrix maximum line candidate count. Zero lets the native SDK choose its default.
    /// </summary>
    public uint DataMatrixMaxLineCandidates { get; set; }

    /// <summary>
    /// Binarization threshold. Zero enables the native automatic threshold behavior.
    /// </summary>
    public uint Threshold { get; set; }

    /// <summary>
    /// Restricts scan orientations. The default value lets the native SDK choose its orientation behavior.
    /// </summary>
    public BarcodeOrientation Orientation { get; set; }

    /// <summary>
    /// Maximum skew correction in degrees. Zero lets the native SDK choose its default.
    /// </summary>
    public uint MaxSkewDegrees { get; set; }

    /// <summary>
    /// Linear barcode light margin. Zero lets the native SDK choose its default.
    /// </summary>
    public uint LightMargin { get; set; }

    /// <summary>
    /// Linear barcode scan distance. Zero lets the native SDK choose its default.
    /// </summary>
    public uint ScanDistanceBarcode { get; set; }

    /// <summary>
    /// Linear barcode tolerance. Zero lets the native SDK choose its default.
    /// </summary>
    public uint Tolerance { get; set; }

    /// <summary>
    /// Minimum barcode height. Zero lets the native SDK choose its default.
    /// </summary>
    public uint MinHeight { get; set; }

    /// <summary>
    /// Engine-specific percentage option. Zero lets the native SDK choose its default.
    /// </summary>
    public uint Percent { get; set; }

    /// <summary>
    /// Engine-specific scan distance. Zero lets the native SDK choose its default.
    /// </summary>
    public uint ScanDistance { get; set; }

    /// <summary>
    /// Maximum allowed gap. Zero lets the native SDK choose its default.
    /// </summary>
    public uint MaxGap { get; set; }

    /// <summary>
    /// Maximum barcode height. Zero lets the native SDK choose its default.
    /// </summary>
    public uint MaxHeight { get; set; }

    /// <summary>
    /// Engine-specific checksum flags.
    /// </summary>
    public uint ChecksumFlags { get; set; }

    /// <summary>
    /// Whole native scan timeout in milliseconds. Zero disables the timeout.
    /// </summary>
    public uint ScanTimeoutMs { get; set; }

    /// <summary>
    /// Encoding used for <see cref="BarcodeResult.Text"/>. When unset, UTF-8 is tried first and legacy Windows-1252 is used as fallback.
    /// </summary>
    public Encoding? TextEncoding { get; set; }

    /// <summary>
    /// Creates a detached copy that can safely be used by asynchronous scans.
    /// </summary>
    public BarcodeReaderOptions Clone()
    {
        return new BarcodeReaderOptions
        {
            Symbologies = Symbologies,
            MinLength = MinLength,
            Flags = Flags,
            PageStart = PageStart,
            PageCount = PageCount,
            Dpi = Dpi,
            DataMatrixFinderAngleTolerance = DataMatrixFinderAngleTolerance,
            DataMatrixOverlapPercent = DataMatrixOverlapPercent,
            DataMatrixMaxLineCandidates = DataMatrixMaxLineCandidates,
            Threshold = Threshold,
            Orientation = Orientation,
            MaxSkewDegrees = MaxSkewDegrees,
            LightMargin = LightMargin,
            ScanDistanceBarcode = ScanDistanceBarcode,
            Tolerance = Tolerance,
            MinHeight = MinHeight,
            Percent = Percent,
            ScanDistance = ScanDistance,
            MaxGap = MaxGap,
            MaxHeight = MaxHeight,
            ChecksumFlags = ChecksumFlags,
            ScanTimeoutMs = ScanTimeoutMs,
            TextEncoding = TextEncoding
        };
    }

    internal NativeScanOptions ToNative()
    {
        Validate();

        var native = new NativeScanOptions();
        var initStatus = NativeMethods.Invoke(() => NativeMethods.qsbc_loader_scan_options_init(ref native));
        if (initStatus < 0)
        {
            throw new BarcodeScanException(initStatus, NativeMethods.PtrToString(NativeMethods.qsbc_loader_status_name(initStatus)));
        }

        ApplyToNative(ref native);

        return native;
    }

    internal void ApplyToNative(ref NativeScanOptions native)
    {
        Validate();

        native.StructSize = (uint)Marshal.SizeOf<NativeScanOptions>();
        native.Mask = (int)Symbologies;
        native.MinLength = MinLength == 0 ? 1 : MinLength;
        native.Flags = (uint)Flags;
        native.PageStart = PageStart;
        native.PageCount = PageCount;
        native.Dpi = Dpi;
        native.Reserved0 = DataMatrixFinderAngleTolerance;
        native.Reserved1 = DataMatrixOverlapPercent;
        native.Reserved2 = DataMatrixMaxLineCandidates;
        native.Threshold = Threshold;
        native.Orientation = (uint)Orientation;
        native.MaxSkewDegrees = MaxSkewDegrees;
        native.LightMargin = LightMargin;
        native.ScanDistanceBarcode = ScanDistanceBarcode;
        native.Tolerance = Tolerance;
        native.MinHeight = MinHeight;
        native.Percent = Percent;
        native.ScanDistance = ScanDistance;
        native.MaxGap = MaxGap;
        native.MaxHeight = MaxHeight;
        native.ChecksumFlags = ChecksumFlags;
        native.ScanTimeoutMs = ScanTimeoutMs;
    }

    internal void Validate()
    {
        if (PageStart < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(PageStart), PageStart, "PageStart must be -1 for native default behavior or a zero-based page index.");
        }

        if (PageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PageCount), PageCount, "PageCount must be zero for all pages or a positive page count.");
        }
    }
}
