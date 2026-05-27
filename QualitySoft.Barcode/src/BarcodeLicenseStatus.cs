using System;
using System.Collections.Generic;

namespace QualitySoft.Barcode;

/// <summary>
/// Interpreted license status returned by the native QS Barcode engine.
/// </summary>
public readonly struct BarcodeLicenseStatus : IEquatable<BarcodeLicenseStatus>
{
    public BarcodeLicenseStatus(int rawValue)
    {
        RawValue = rawValue;
        Features = (BarcodeLicenseFeatures)rawValue;
    }

    /// <summary>
    /// Raw native license bit mask.
    /// </summary>
    public int RawValue { get; }

    /// <summary>
    /// Interpreted license feature flags.
    /// </summary>
    public BarcodeLicenseFeatures Features { get; }

    /// <summary>
    /// Indicates that the native SDK is running in demo mode.
    /// </summary>
    public bool IsDemo => HasFeature(BarcodeLicenseFeatures.Demo);

    /// <summary>
    /// Indicates that linear and postal barcode scanning is licensed.
    /// </summary>
    public bool AllowsLinear => HasFeature(BarcodeLicenseFeatures.Linear);

    /// <summary>
    /// Indicates that PDF417 scanning is licensed.
    /// </summary>
    public bool AllowsPdf417 => HasFeature(BarcodeLicenseFeatures.Pdf417);

    /// <summary>
    /// Indicates that DataMatrix scanning is licensed.
    /// </summary>
    public bool AllowsDataMatrix => HasFeature(BarcodeLicenseFeatures.DataMatrix);

    /// <summary>
    /// Indicates that QR Code scanning is licensed.
    /// </summary>
    public bool AllowsQr => HasFeature(BarcodeLicenseFeatures.Qr);

    /// <summary>
    /// Indicates that Aztec scanning is licensed.
    /// </summary>
    public bool AllowsAztec => HasFeature(BarcodeLicenseFeatures.Aztec);

    /// <summary>
    /// Returns true when all bits from <paramref name="feature"/> are present.
    /// </summary>
    public bool HasFeature(BarcodeLicenseFeatures feature)
    {
        return (Features & feature) == feature;
    }

    /// <summary>
    /// Returns true when the current license covers all requested symbologies.
    /// </summary>
    public bool CanScan(BarcodeSymbology symbologies)
    {
        return MissingFeaturesFor(symbologies) == BarcodeLicenseFeatures.None;
    }

    /// <summary>
    /// Returns the missing license features for the requested symbology mask.
    /// </summary>
    public BarcodeLicenseFeatures MissingFeaturesFor(BarcodeSymbology symbologies)
    {
        var requested = symbologies == BarcodeSymbology.None
            ? BarcodeSymbology.Default
            : symbologies;

        var missing = BarcodeLicenseFeatures.None;

        if ((requested & BarcodeSymbology.LinearMask) != 0 && !AllowsLinear)
        {
            missing |= BarcodeLicenseFeatures.Linear;
        }

        if ((requested & BarcodeSymbology.Postal) != 0 && !AllowsLinear)
        {
            missing |= BarcodeLicenseFeatures.Linear;
        }

        if ((requested & BarcodeSymbology.DataMatrix) != 0 && !AllowsDataMatrix)
        {
            missing |= BarcodeLicenseFeatures.DataMatrix;
        }

        if ((requested & BarcodeSymbology.Qr) != 0 && !AllowsQr)
        {
            missing |= BarcodeLicenseFeatures.Qr;
        }

        if ((requested & BarcodeSymbology.Aztec) != 0 && !AllowsAztec)
        {
            missing |= BarcodeLicenseFeatures.Aztec;
        }

        if ((requested & BarcodeSymbology.Pdf417) != 0 && !AllowsPdf417)
        {
            missing |= BarcodeLicenseFeatures.Pdf417;
        }

        return missing;
    }

    /// <summary>
    /// Returns the missing license features as a stable ordered list for UI and diagnostics.
    /// </summary>
    public IReadOnlyList<BarcodeLicenseFeatures> MissingFeatureListFor(BarcodeSymbology symbologies)
    {
        var missing = MissingFeaturesFor(symbologies);
        if (missing == BarcodeLicenseFeatures.None)
        {
            return Array.Empty<BarcodeLicenseFeatures>();
        }

        var result = new List<BarcodeLicenseFeatures>(5);
        AddIfMissing(result, missing, BarcodeLicenseFeatures.Linear);
        AddIfMissing(result, missing, BarcodeLicenseFeatures.Pdf417);
        AddIfMissing(result, missing, BarcodeLicenseFeatures.DataMatrix);
        AddIfMissing(result, missing, BarcodeLicenseFeatures.Qr);
        AddIfMissing(result, missing, BarcodeLicenseFeatures.Aztec);
        return result;
    }

    public override string ToString()
    {
        return Features.ToString();
    }

    public bool Equals(BarcodeLicenseStatus other)
    {
        return RawValue == other.RawValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is BarcodeLicenseStatus other && Equals(other);
    }

    public override int GetHashCode()
    {
        return RawValue;
    }

    public static bool operator ==(BarcodeLicenseStatus left, BarcodeLicenseStatus right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BarcodeLicenseStatus left, BarcodeLicenseStatus right)
    {
        return !left.Equals(right);
    }

    private static void AddIfMissing(ICollection<BarcodeLicenseFeatures> result, BarcodeLicenseFeatures missing, BarcodeLicenseFeatures feature)
    {
        if ((missing & feature) == feature)
        {
            result.Add(feature);
        }
    }
}
