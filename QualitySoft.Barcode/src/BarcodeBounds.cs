using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Barcode bounds in rendered image coordinates.
/// </summary>
public readonly struct BarcodeBounds : IEquatable<BarcodeBounds>
{
    /// <summary>
    /// Creates barcode bounds in rendered image coordinates.
    /// </summary>
    public BarcodeBounds(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Left coordinate in pixels.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Top coordinate in pixels.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public int Height { get; }

    public bool Equals(BarcodeBounds other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is BarcodeBounds other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = X;
            hash = (hash * 397) ^ Y;
            hash = (hash * 397) ^ Width;
            hash = (hash * 397) ^ Height;
            return hash;
        }
    }

    public override string ToString()
    {
        return X + "," + Y + " " + Width + "x" + Height;
    }
}
