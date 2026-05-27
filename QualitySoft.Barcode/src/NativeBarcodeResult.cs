using System;
using System.Runtime.InteropServices;

namespace QualitySoft.Barcode;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeBarcodeResult
{
    public int Format;
    public int SymbologyMask;
    public IntPtr Text;
    public int TextLen;
    public uint Width;
    public uint Height;
    public uint ImageIndex;
    public int PageIndex;
    public int BarcodeX;
    public int BarcodeY;
    public int BarcodeWidth;
    public int BarcodeHeight;
    public uint HasBounds;
    public uint Reserved0;
    public uint Reserved1;
    public uint Reserved2;
}

