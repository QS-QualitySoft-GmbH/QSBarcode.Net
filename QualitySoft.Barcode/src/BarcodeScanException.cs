using System;

namespace QualitySoft.Barcode;

/// <summary>
/// Raised when the native loader returns an error status.
/// </summary>
public sealed class BarcodeScanException : Exception
{
    /// <summary>
    /// Creates an exception for a native scan status.
    /// </summary>
    public BarcodeScanException(int statusCode, string? statusName)
        : this(statusCode, statusName, null, null, BarcodeLicenseFeatures.None)
    {
    }

    /// <summary>
    /// Creates an exception for a native scan status with license diagnostics.
    /// </summary>
    public BarcodeScanException(
        int statusCode,
        string? statusName,
        BarcodeSymbology? requestedSymbologies,
        BarcodeLicenseStatus? licenseStatus,
        BarcodeLicenseFeatures missingFeatures)
        : base(CreateMessage(statusCode, statusName, requestedSymbologies, licenseStatus, missingFeatures))
    {
        StatusCode = statusCode;
        StatusName = statusName;
        RequestedSymbologies = requestedSymbologies;
        LicenseStatus = licenseStatus;
        MissingLicenseFeatures = missingFeatures;
    }

    /// <summary>
    /// Numeric status code returned by the native SDK.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Native status name when available.
    /// </summary>
    public string? StatusName { get; }

    /// <summary>
    /// Symbology mask requested by the scan when available.
    /// </summary>
    public BarcodeSymbology? RequestedSymbologies { get; }

    /// <summary>
    /// Native license status observed when the error was created.
    /// </summary>
    public BarcodeLicenseStatus? LicenseStatus { get; }

    /// <summary>
    /// License feature bits missing for the requested symbologies.
    /// </summary>
    public BarcodeLicenseFeatures MissingLicenseFeatures { get; }

    private static string CreateMessage(
        int statusCode,
        string? statusName,
        BarcodeSymbology? requestedSymbologies,
        BarcodeLicenseStatus? licenseStatus,
        BarcodeLicenseFeatures missingFeatures)
    {
        if (statusCode == NativeMethods.StatusLicenseRequired)
        {
            var requested = requestedSymbologies.HasValue ? requestedSymbologies.Value.ToString() : "unknown";
            var features = licenseStatus.HasValue ? licenseStatus.Value.Features.ToString() : "unknown";
            var missing = missingFeatures == BarcodeLicenseFeatures.None ? "unknown" : missingFeatures.ToString();
            return "Native barcode scan requires additional license features. Requested symbologies: " + requested + ". Current license features: " + features + ". Missing features: " + missing + ".";
        }

        return "Native barcode scan failed with status " + statusCode + " (" + (statusName ?? "unknown") + ").";
    }
}
