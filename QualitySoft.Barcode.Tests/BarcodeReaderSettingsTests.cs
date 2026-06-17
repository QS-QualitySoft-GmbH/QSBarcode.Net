using Xunit;

namespace QualitySoft.Barcode.Tests;

public sealed class BarcodeReaderSettingsTests
{
    [Fact]
    public void Clone_DetachesDefaultOptions()
    {
        var settings = new BarcodeReaderSettings
        {
            DefaultOptions = new BarcodeReaderOptions
            {
                Dpi = 300
            }
        };

        var clone = settings.Clone();
        settings.DefaultOptions.Dpi = 200;

        Assert.Equal(300u, clone.DefaultOptions.Dpi);
        Assert.NotSame(settings.DefaultOptions, clone.DefaultOptions);
    }

    [Fact]
    public void Constructor_RejectsNullDefaultOptions()
    {
        var settings = new BarcodeReaderSettings
        {
            DefaultOptions = null!
        };

        Assert.Throws<ArgumentNullException>(() => new QualitySoftBarcodeReader(settings));
    }
}
