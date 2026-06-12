using System;
using System.Runtime.InteropServices;

namespace QualitySoft.Barcode;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeImageBuffer
{
    public IntPtr Data;
    public UIntPtr Len;
    public uint Width;
    public uint Height;
    public int Format;
    public int PageIndex;
    public uint Reserved0;
    public uint Reserved1;
}
