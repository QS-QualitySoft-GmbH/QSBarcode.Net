using System.Text;
using Xunit;

namespace QualitySoft.Barcode.Tests;

public sealed class BarcodeValueTests
{
    [Fact]
    public void BarcodeRenderedImage_StoresManagedCopyMetadata()
    {
        var bytes = new byte[] { 1, 2, 3 };

        var image = new BarcodeRenderedImage(bytes, 100, 50, BarcodeImageFormat.Pdf, 2);

        Assert.Same(bytes, image.BmpBytes);
        Assert.Same(bytes, image.Bytes);
        Assert.Equal(100u, image.Width);
        Assert.Equal(50u, image.Height);
        Assert.Equal(BarcodeImageFormat.Pdf, image.SourceFormat);
        Assert.Equal(2, image.PageIndex);
        Assert.Equal(BarcodeRenderedPixelFormat.Bmp24, image.PixelFormat);
        Assert.Equal(0u, image.Stride);
    }

    [Fact]
    public void BarcodeResult_DecodesUtf8ByDefault()
    {
        var result = new BarcodeResult(
            BarcodeImageFormat.Png,
            BarcodeSymbology.Qr,
            Encoding.UTF8.GetBytes("Gruesse"),
            imageWidth: 10,
            imageHeight: 20,
            imageIndex: 0,
            pageIndex: -1,
            bounds: null,
            textEncoding: null);

        Assert.Equal("Gruesse", result.Text);
        Assert.Equal("Gruesse", result.ToString());
    }

    [Fact]
    public void BarcodeNativeCapabilities_CanRepresentFormatSupportFlags()
    {
        var capabilities = BarcodeNativeCapabilities.MemoryScan | BarcodeNativeCapabilities.Pdf | BarcodeNativeCapabilities.Tiff;

        Assert.True(capabilities.HasFlag(BarcodeNativeCapabilities.MemoryScan));
        Assert.True(capabilities.HasFlag(BarcodeNativeCapabilities.Pdf));
        Assert.False(capabilities.HasFlag(BarcodeNativeCapabilities.Jpeg));
    }
}
