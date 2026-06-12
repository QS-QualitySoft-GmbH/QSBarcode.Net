using Xunit;

namespace QualitySoft.Barcode.Tests;

public sealed class BarcodeReaderSettingsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 4)]
    public void GetEffectivePdfRenderWorkerWarmupCount_DerivesFromMaxConcurrentScans(int maxConcurrentScans, int expectedWarmup)
    {
        var settings = new BarcodeReaderSettings
        {
            MaxConcurrentScans = maxConcurrentScans
        };

        Assert.Equal(expectedWarmup, settings.GetEffectivePdfRenderWorkerWarmupCount());
    }

    [Fact]
    public void GetEffectivePdfRenderWorkerWarmupCount_UsesExplicitZeroForLazyStartup()
    {
        var settings = new BarcodeReaderSettings
        {
            MaxConcurrentScans = 4,
            PdfRenderWorkerWarmupCount = 0
        };

        Assert.Equal(0, settings.GetEffectivePdfRenderWorkerWarmupCount());
    }

    [Fact]
    public void Clone_CopiesAsyncByteArrayCopyPolicy()
    {
        var settings = new BarcodeReaderSettings
        {
            CopyInputBuffersForAsyncByteArray = false
        };

        var clone = settings.Clone();

        Assert.False(clone.CopyInputBuffersForAsyncByteArray);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-2)]
    public void Constructor_RejectsInvalidPdfRenderWorkerWarmupCount(int invalidWarmup)
    {
        var settings = new BarcodeReaderSettings
        {
            PdfRenderWorkerWarmupCount = invalidWarmup
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new QualitySoftBarcodeReader(settings));
    }

    [Fact]
    public void Constructor_RejectsInvalidMaxConcurrentScans()
    {
        var settings = new BarcodeReaderSettings
        {
            MaxConcurrentScans = 0,
            PdfRenderWorkerWarmupCount = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new QualitySoftBarcodeReader(settings));
    }
}
