using System;
using System.Text;

namespace QualitySoft.Barcode;

/// <summary>
/// One decoded barcode result returned by the native SDK.
/// </summary>
public sealed class BarcodeResult
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding LegacyTextEncoding = CreateLegacyTextEncoding();

    internal BarcodeResult(
        BarcodeImageFormat format,
        BarcodeSymbology symbology,
        byte[] bytes,
        uint imageWidth,
        uint imageHeight,
        uint imageIndex,
        int pageIndex,
        BarcodeBounds? bounds,
        Encoding? textEncoding)
    {
        Format = format;
        Symbology = symbology;
        Bytes = bytes;
        Text = DecodeText(bytes, textEncoding);
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        ImageIndex = imageIndex;
        PageIndex = pageIndex;
        Bounds = bounds;
    }

    /// <summary>
    /// Input image format reported by the native loader.
    /// </summary>
    public BarcodeImageFormat Format { get; }

    /// <summary>
    /// Symbology of the decoded barcode.
    /// </summary>
    public BarcodeSymbology Symbology { get; }

    /// <summary>
    /// Raw decoded payload bytes exactly as returned by the native engine.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Decoded payload text. UTF-8 is used when valid; otherwise Windows-1252 is used unless an explicit text encoding was configured.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Width of the source image or rendered page that produced this result.
    /// </summary>
    public uint ImageWidth { get; }

    /// <summary>
    /// Height of the source image or rendered page that produced this result.
    /// </summary>
    public uint ImageHeight { get; }

    /// <summary>
    /// Zero-based image index for multi-image inputs.
    /// </summary>
    public uint ImageIndex { get; }

    /// <summary>
    /// Zero-based page index for page-based inputs, or the native engine value for single image inputs.
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Barcode bounds in rendered image coordinates when reported by the native engine.
    /// </summary>
    public BarcodeBounds? Bounds { get; }

    public override string ToString()
    {
        return Text;
    }

    private static string DecodeText(byte[] bytes, Encoding? textEncoding)
    {
        if (textEncoding != null)
        {
            return textEncoding.GetString(bytes, 0, bytes.Length);
        }

        try
        {
            return StrictUtf8.GetString(bytes, 0, bytes.Length);
        }
        catch (DecoderFallbackException)
        {
            return LegacyTextEncoding.GetString(bytes, 0, bytes.Length);
        }
    }

    private static Encoding CreateLegacyTextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding(1252);
        }
        catch (ArgumentException)
        {
            return Encoding.GetEncoding("iso-8859-1");
        }
    }
}
