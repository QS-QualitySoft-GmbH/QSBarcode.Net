using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Rendered page or frame returned by the native loader.
/// </summary>
public sealed class BarcodeRenderedImage
{
    internal BarcodeRenderedImage(byte[] bmpBytes, uint width, uint height, BarcodeImageFormat sourceFormat, int pageIndex)
        : this(bmpBytes, width, height, sourceFormat, pageIndex, BarcodeRenderedPixelFormat.Bmp24, 0)
    {
    }

    internal BarcodeRenderedImage(byte[] bytes, uint width, uint height, BarcodeImageFormat sourceFormat, int pageIndex, BarcodeRenderedPixelFormat pixelFormat, uint stride)
    {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Width = width;
        Height = height;
        SourceFormat = sourceFormat;
        PageIndex = pageIndex;
        PixelFormat = pixelFormat;
        Stride = stride == 0 && pixelFormat == BarcodeRenderedPixelFormat.Gray8 ? width : stride;
    }

    /// <summary>
    /// Rendered bytes. For <see cref="BarcodeRenderedPixelFormat.Bmp24"/>, this is a complete BMP file. For <see cref="BarcodeRenderedPixelFormat.Gray8"/>, this is raw tightly packed grayscale pixels.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Complete 24-bit BMP file bytes for the rendered page or frame. This is equivalent to <see cref="Bytes"/> for BMP renders.
    /// </summary>
    public byte[] BmpBytes => Bytes;

    /// <summary>
    /// Width of the rendered image in pixels.
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Height of the rendered image in pixels.
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Source input format that was rendered.
    /// </summary>
    public BarcodeImageFormat SourceFormat { get; }

    /// <summary>
    /// Zero-based page or frame index for PDF, TIFF and GIF inputs; otherwise -1.
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Pixel representation stored in <see cref="Bytes"/>.
    /// </summary>
    public BarcodeRenderedPixelFormat PixelFormat { get; }

    /// <summary>
    /// Row stride in bytes for raw pixel formats, or zero for complete file formats such as BMP.
    /// </summary>
    public uint Stride { get; }
}
