using Xunit;

namespace QualitySoft.Barcode.Tests;

public sealed class QualitySoftBarcodeReaderValidationTests
{
    [Fact]
    public void Read_RejectsEmptyByteArray()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentException>(() => reader.Read(Array.Empty<byte>()));
    }

    [Fact]
    public void Read_RejectsNullPointer()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentNullException>(() => reader.Read(IntPtr.Zero, 10));
    }

    [Fact]
    public void Read_RejectsInvalidPointerLength()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new IntPtr(1), 0));
    }

    [Fact]
    public void ReadOnlySpanOverload_RejectsEmptyInput()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentException>(() => reader.Read(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ReadOnlyMemoryOverload_RejectsEmptyInput()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentException>(() => reader.Read(ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public async Task ReadAsyncReadOnlyMemoryOverload_RejectsEmptyInput()
    {
        using var reader = CreateReader();

        await Assert.ThrowsAsync<ArgumentException>(() => reader.ReadAsync(ReadOnlyMemory<byte>.Empty));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10, 0, 0)]
    [InlineData(10, 10, -1)]
    [InlineData(10, 10, 9)]
    public void ReadRawGray8_RejectsInvalidShape(int width, int height, int stride)
    {
        using var reader = CreateReader();

        Assert.ThrowsAny<ArgumentException>(() => reader.ReadRawGray8(new byte[100], width, height, stride));
    }

    [Fact]
    public void ReadRawGray8_RejectsBufferSmallerThanStrideRequires()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentException>(() => reader.ReadRawGray8(new byte[19], width: 5, height: 4, stride: 6));
    }

    [Fact]
    public void ReadRawGray8_RejectsNullPointer()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentNullException>(() => reader.ReadRawGray8(IntPtr.Zero, width: 10, height: 10));
    }

    [Fact]
    public void ReadRawPixels_RejectsBufferSmallerThanFormatRequires()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentException>(() => reader.ReadRawPixels(new byte[29], width: 10, height: 1, BarcodeRawPixelFormat.Rgb24));
    }

    [Fact]
    public void ReadRawPixels_RejectsInvalidFormat()
    {
        using var reader = CreateReader();

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadRawPixels(new byte[100], width: 10, height: 10, (BarcodeRawPixelFormat)999));
    }

    private static QualitySoftBarcodeReader CreateReader()
    {
        return new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
            PdfRenderWorkerWarmupCount = 0
        });
    }
}
