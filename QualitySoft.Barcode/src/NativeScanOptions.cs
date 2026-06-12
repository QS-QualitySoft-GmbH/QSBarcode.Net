using System.Runtime.InteropServices;

namespace QualitySoft.Barcode;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeScanOptions
{
    public uint StructSize;
    public int Mask;
    public uint MinLength;
    public uint ReservedResultLimit;
    public uint Flags;
    public int PageStart;
    public int PageCount;
    public uint Dpi;
    public uint Reserved0;
    public uint Reserved1;
    public uint Reserved2;
    public uint Reserved3;
    public uint Threshold;
    public uint Orientation;
    public uint MaxSkewDegrees;
    public uint LightMargin;
    public uint ScanDistanceBarcode;
    public uint Tolerance;
    public uint MinHeight;
    public uint Percent;
    public uint ScanDistance;
    public uint MaxGap;
    public uint MaxHeight;
    public uint ChecksumFlags;
    public uint ScanTimeoutMs;
}
