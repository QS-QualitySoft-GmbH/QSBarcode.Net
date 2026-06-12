using Xunit;

namespace QualitySoft.Barcode.Tests;

public sealed class BarcodeReaderOptionsTests
{
    [Fact]
    public void Clone_CopiesAllManagedOptions()
    {
        var options = new BarcodeReaderOptions
        {
            Symbologies = BarcodeSymbology.DataMatrix | BarcodeSymbology.Code128,
            MinLength = 7,
            Flags = BarcodeScanFlags.DataMatrixIntensiveSearch,
            PageStart = 2,
            PageCount = 3,
            Dpi = 300,
            DataMatrixFinderAngleTolerance = 11,
            DataMatrixOverlapPercent = 12,
            DataMatrixMaxLineCandidates = 13,
            Threshold = 128,
            Orientation = BarcodeOrientation.Degrees90,
            MaxSkewDegrees = 21,
            LightMargin = 4,
            ScanDistanceBarcode = 5,
            Tolerance = 6,
            MinHeight = 20,
            Percent = 80,
            ScanDistance = 9,
            MaxGap = 10,
            MaxHeight = 200,
            ChecksumFlags = 1,
            ScanTimeoutMs = 1500,
            TextEncoding = System.Text.Encoding.ASCII
        };

        var clone = options.Clone();

        Assert.NotSame(options, clone);
        Assert.Equal(options.Symbologies, clone.Symbologies);
        Assert.Equal(options.MinLength, clone.MinLength);
        Assert.Equal(options.Flags, clone.Flags);
        Assert.Equal(options.PageStart, clone.PageStart);
        Assert.Equal(options.PageCount, clone.PageCount);
        Assert.Equal(options.Dpi, clone.Dpi);
        Assert.Equal(options.DataMatrixFinderAngleTolerance, clone.DataMatrixFinderAngleTolerance);
        Assert.Equal(options.DataMatrixOverlapPercent, clone.DataMatrixOverlapPercent);
        Assert.Equal(options.DataMatrixMaxLineCandidates, clone.DataMatrixMaxLineCandidates);
        Assert.Equal(options.Threshold, clone.Threshold);
        Assert.Equal(options.Orientation, clone.Orientation);
        Assert.Equal(options.MaxSkewDegrees, clone.MaxSkewDegrees);
        Assert.Equal(options.LightMargin, clone.LightMargin);
        Assert.Equal(options.ScanDistanceBarcode, clone.ScanDistanceBarcode);
        Assert.Equal(options.Tolerance, clone.Tolerance);
        Assert.Equal(options.MinHeight, clone.MinHeight);
        Assert.Equal(options.Percent, clone.Percent);
        Assert.Equal(options.ScanDistance, clone.ScanDistance);
        Assert.Equal(options.MaxGap, clone.MaxGap);
        Assert.Equal(options.MaxHeight, clone.MaxHeight);
        Assert.Equal(options.ChecksumFlags, clone.ChecksumFlags);
        Assert.Equal(options.ScanTimeoutMs, clone.ScanTimeoutMs);
        Assert.Same(options.TextEncoding, clone.TextEncoding);
    }

    [Fact]
    public void ToNative_CopiesScanTimeoutMs()
    {
        var options = new BarcodeReaderOptions
        {
            ScanTimeoutMs = 2500
        };
        var native = new NativeScanOptions();

        options.ApplyToNative(ref native);

        Assert.Equal(2500u, native.ScanTimeoutMs);
    }

    [Theory]
    [InlineData(-2, 0)]
    [InlineData(-1, -1)]
    public void Validate_RejectsInvalidPageRanges(int pageStart, int pageCount)
    {
        var options = new BarcodeReaderOptions
        {
            PageStart = pageStart,
            PageCount = pageCount
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }
}
