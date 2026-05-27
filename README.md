# QualitySoft.Barcode .NET SDK

`QualitySoft.Barcode` is the official .NET wrapper for the QS Barcode native
engine. It gives .NET applications a clean managed API for barcode recognition
while the performance-critical scanning, image handling and license enforcement
remain in the native QS Barcode runtime.

The SDK is distributed as the `QualitySoft.Barcode` NuGet package.

## What It Does

The QS Barcode engine is built for document and image based barcode recognition
in business applications. It can scan barcodes from image files, image streams,
raw byte arrays and PDF documents.

Supported feature groups include:

- 1D / linear barcodes such as Code 128, Code 39, EAN/UPC, GS1-128 and related
  industrial formats
- PDF417
- Data Matrix
- QR Code
- Aztec
- multi-page PDF input
- cross-platform native execution on Windows, Linux and macOS
- license-aware feature checks for demo, evaluation and commercial deployments

The public repository contains the .NET wrapper and API surface. The proprietary
native engine is delivered through the NuGet package as platform-specific native
runtime assets.

## Installation

NuGet package:
`https://www.nuget.org/packages/QualitySoft.Barcode`

Install with the .NET CLI:

```powershell
dotnet add package QualitySoft.Barcode --version 0.3.0
```

Or add a `PackageReference` manually:

```xml
<PackageReference Include="QualitySoft.Barcode" Version="0.3.0" />
```

Visual Studio Package Manager Console:

```powershell
Install-Package QualitySoft.Barcode -Version 0.3.0
```

Product page, pricing and documentation:
`https://qualitysoft.de/products/qs-barcode-sdk/`

## Quick Start

```csharp
using QualitySoft.Barcode;

IBarcodeReader reader = new QualitySoftBarcodeReader();

var results = await reader.ReadAsync(
    "invoice.pdf",
    new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.Code128 | BarcodeSymbology.DataMatrix,
        Dpi = 300,
        MinLength = 4
    });

foreach (var result in results)
{
    Console.WriteLine($"{result.Symbology}: {result.Text}");
}
```

Async scans take a snapshot of `BarcodeReaderOptions` before native work starts.
Cancellation is observed before the native scan starts and while stream data is
copied. Once native scanning is running, the native call is allowed to finish on
its background scan thread; the returned task can still be canceled so request
handling does not have to wait for native completion.

## Supported Barcode Types

The wrapper exposes the native scan mask through the `[Flags]`
`BarcodeSymbology` enum, so barcode types can be combined with `|`.

| Group | `BarcodeSymbology` values |
| --- | --- |
| Common 1D / linear | `Code128`, `Ean128`, `Code39`, `Code39Ext`, `Code32`, `Code11`, `Codabar`, `Code93`, `Code93Ext` |
| Interleaved and industrial 2 of 5 | `I25`, `Industrial25`, `Iata25`, `Inverted25`, `Matrix25`, `Datalogic25`, `BcdMatrix25` |
| EAN / UPC | `Ean8`, `Ean13`, `Upca`, `Upce` |
| GS1 DataBar | `Databar`, `DatabarOmni`, `DatabarExpanded`, `DatabarLimited` |
| Other linear families | `Codablock`, `Pharma`, `Patch`, `Postal` |
| 2D | `DataMatrix`, `Qr`, `Aztec`, `Pdf417` |
| Presets | `NativeDefault`, `Default`, `LinearMask`, `TwoDimensionalMask`, `All` |

`NativeDefault` passes mask `0` to the native engine. `Default` is the managed
common-use preset and excludes the more specialized `Pharma`, `Patch` and
`Postal` families. Exact feature availability can depend on the installed QS
Barcode license.

## Reader Settings

`BarcodeReaderOptions` is designed so unset values preserve native SDK defaults.
For most applications, setting `Symbologies`, `Dpi`, `MinLength` and optional
page ranges is enough.

| Option | Default | Purpose |
| --- | --- | --- |
| `Symbologies` | `NativeDefault` | Barcode mask to scan. Combine values with `|`, for example `Code128 | DataMatrix`. |
| `MinLength` | `1` | Minimum decoded text length. Values below `1` are normalized to `1`. |
| `PageStart` | `-1` | Zero-based first page for PDF/TIFF inputs. `-1` starts at the first page. |
| `PageCount` | `0` | Number of pages to scan. `0` means all remaining pages. |
| `Dpi` | `0` | PDF render DPI. `0` keeps the native default. Use `200` or `300` for many document workflows. |
| `Threshold` | `0` | Binarization threshold. `0` keeps automatic thresholding. |
| `Orientation` | `Default` | Restricts scan orientation when needed. |
| `Flags` | `None` | Optional engine flags for Data Matrix and QR tuning. |
| `TextEncoding` | auto | Decodes `BarcodeResult.Text`. UTF-8 is tried first, then Windows-1252 fallback unless an encoding is supplied. |
| `DataMatrixFinderAngleTolerance` | `0` | Data Matrix finder angle tuning. `0` keeps the native default. |
| `DataMatrixOverlapPercent` | `0` | Data Matrix overlap tuning. `0` keeps the native default. |
| `DataMatrixMaxLineCandidates` | `0` | Data Matrix candidate limit. `0` keeps the native default. |
| `MaxSkewDegrees` | `0` | Maximum skew correction in degrees. `0` keeps the native default. |
| `LightMargin` | `0` | Linear barcode quiet-zone/light-margin tuning. |
| `ScanDistanceBarcode` | `0` | Linear barcode scan-distance tuning. |
| `Tolerance` | `0` | Linear barcode tolerance tuning. |
| `MinHeight` | `0` | Minimum barcode height. |
| `MaxHeight` | `0` | Maximum barcode height. |
| `Percent` | `0` | Legacy engine percentage option. |
| `ScanDistance` | `0` | Legacy engine scan-distance option. |
| `MaxGap` | `0` | Maximum allowed gap for linear decoding. |
| `ChecksumFlags` | `0` | Engine-specific checksum behavior. |

Orientation values:

| `BarcodeOrientation` value | Meaning |
| --- | --- |
| `Default` | Let the native engine decide. |
| `Degrees0`, `Degrees90`, `Degrees180`, `Degrees270` | Scan only one orientation. |
| `Degrees0And180`, `Degrees90And270` | Scan paired orientations. |
| `All` | Scan all four main orientations. |

Scan flags:

| `BarcodeScanFlags` value | Purpose |
| --- | --- |
| `DataMatrixReportSymbolIdentifier` | Include Data Matrix symbol identifier behavior from the native engine. |
| `DataMatrixSuppressEci` | Suppress Data Matrix ECI handling. |
| `DataMatrixIntensiveSearch` | Enable more intensive Data Matrix search. |
| `DataMatrixSearchOnDoubledRegion` | Search Data Matrix on doubled regions. |
| `DataMatrixZebraDoubling` | Enable Data Matrix zebra doubling mode. |
| `DataMatrixTryErodedImage` | Try eroded-image Data Matrix decoding. |
| `QrEci` | Enable QR ECI handling. |
| `QrDoubleImage` | Enable QR double-image mode. |

## Architecture

The SDK is intentionally split into a small managed wrapper and a native runtime:

```text
.NET application
  |
  | QualitySoft.Barcode managed API
  v
IBarcodeReader / BarcodeReaderOptions / BarcodeLicense
  |
  | P/Invoke boundary
  v
QS Barcode native loader
  |
  | image/PDF decoding, scan dispatch, license checks
  v
QS Barcode native engine + PDFium runtime
```

Managed responsibilities:

- .NET-friendly reader API
- sync and async overloads
- stream, file and byte-array handling
- strongly typed scan options
- result objects with text, bytes, symbology, page and bounds metadata
- dependency injection registration
- license status helpers and license-specific exceptions

Native responsibilities:

- barcode detection and decoding
- PDF rendering through PDFium
- platform-specific performance work
- native license evaluation and feature enforcement

This keeps application code simple while allowing the scanner to use optimized
native code per platform.

## Supported .NET Targets

The package supports:

- .NET Framework 4.6.2
- .NET Standard 2.0
- .NET Standard 2.1
- .NET Core 3.1
- .NET 6
- .NET 8

.NET 10 applications can consume the package through the existing modern target
frameworks. A dedicated `net10.0` target is not required for the current API.

## Supported Platforms

The NuGet package includes native runtime assets for:

| RID | Platform |
| --- | --- |
| `win-x86` | Windows 32-bit |
| `win-x64` | Windows x64 |
| `win-arm64` | Windows ARM64 |
| `linux-x64` | Linux x64, glibc |
| `linux-arm64` | Linux ARM64 / AWS Graviton, glibc |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |

Android, iOS, WebAssembly and Alpine/musl are not part of this package. Mobile
support should be handled through dedicated MAUI or platform-specific packages.

PDF input is supported on all listed desktop/server RIDs through bundled PDFium
runtime assets. Applications normally do not need to copy native libraries
manually when they restore or publish for one of the supported runtime
identifiers.

## Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using QualitySoft.Barcode;

services.AddQualitySoftBarcode();
```

Then inject `IBarcodeReader` into services, controllers or background workers.

Applications can also register managed default scan options once. Per-call
options still override these defaults.

```csharp
services.AddQualitySoftBarcode(options =>
{
    options.Symbologies = BarcodeSymbology.LinearMask;
    options.MinLength = 4;
    options.Dpi = 300;
});
```

For high-throughput services, configure reader-level concurrency explicitly:

```csharp
services.AddQualitySoftBarcode(new BarcodeReaderSettings
{
    MaxConcurrentScans = Environment.ProcessorCount,
    DefaultOptions = new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.Default,
        MinLength = 4,
        Dpi = 300
    }
});
```

`QualitySoftBarcodeReader` is safe to register as a singleton. It limits
concurrently executing native scans per reader instance, so `ReadAsync` does not
create unbounded dedicated native scan threads under load. `ReadAsync(byte[])`
copies the supplied byte array before queuing native work.

## Native Runtime Health Check

For deployment health checks, call the native runtime diagnostic API at startup:

```csharp
if (!BarcodeNativeLibrary.TryGetVersion(out var version, out var error))
{
    Console.WriteLine(error);
}
else
{
    Console.WriteLine($"QS Barcode native loader ABI: {version}");
    Console.WriteLine($"QS Barcode native engine: {BarcodeNativeLibrary.GetEngineVersion()}");
}
```

`BarcodeNativeLibrary.GetDiagnostics()` returns the current runtime identifier,
expected native library file name and probing locations. For custom deployment
layouts, set `QSBC_NATIVE_LIBRARY` to an absolute path to the native loader.

## Release Notes

### 0.3.0

- Rebuilt native runtime assets for Windows, Linux and macOS.
- Hardened EC/PDF417 native decoding against invalid candidates and
  out-of-range codewords.
- Fixed a PDF417 miss case where scanning a Data Matrix-heavy TIFF with a
  PDF417 mask could terminate the native process instead of returning zero
  results.
- Added a regression fixture for the PDF417 miss path.
- Bumped the native QS Barcode engine license line to `6.0`.
- Added native and .NET diagnostics for the QS Barcode engine version.

## License And Feature Checks

The SDK exposes the native license state so applications can enable or disable
features before starting a scan.

By default, the native runtime searches for `qsbc.lic`. For Linux, macOS,
containers and hosted applications, set `QSBC_LICENSE_FILE` to an absolute
license path before the first scan:

```bash
export QSBC_LICENSE_FILE=/etc/qualitysoft/qsbc.lic
```

The current native engine line is `QS-Barcode SDK 6.0`. Commercial license
files for this rollout should therefore be issued for `QS-Barcode SDK 6.0`.
Older `5.0` license files are treated as outdated by the legacy license check
and fall back to demo behavior.

Applications can also inspect a specific file path explicitly:

```csharp
var status = BarcodeLicense.GetStatus("/etc/qualitysoft/qsbc.lic");

Console.WriteLine(status.IsDemo ? "Demo" : "Licensed");
Console.WriteLine($"Linear: {status.AllowsLinear}");
Console.WriteLine($"PDF417: {status.AllowsPdf417}");
Console.WriteLine($"DataMatrix: {status.AllowsDataMatrix}");
Console.WriteLine($"QR: {status.AllowsQr}");
Console.WriteLine($"Aztec: {status.AllowsAztec}");
```

### Demo Mode

`Demo` is not a production license tier. It is an evaluation mode used when no
valid commercial feature license is active, or when a demo/evaluation license is
used.

A status such as `Demo, Linear` means that linear barcode detection is available
for evaluation. It does not mean that the application has a production 1D
license. In demo mode the native engine may still return barcode results, but
decoded text is deliberately modified before it is returned:

- linear, PDF417 and postal payloads use legacy character substitution
- Data Matrix, QR Code and Aztec payloads additionally receive a `DEMO` marker

Use `status.IsDemo` to keep demo/evaluation behavior out of production
workflows. Production checks should require both the needed feature and
`!status.IsDemo`.

```csharp
if (status.AllowsLinear && !status.IsDemo)
{
    Console.WriteLine("Production linear scanning is licensed.");
}
```

If an application requests only unlicensed symbologies, the native runtime
returns `license_required` and the wrapper throws `BarcodeScanException` with
the missing license features attached.

```csharp
try
{
    await reader.ReadAsync("label.png", new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.DataMatrix
    });
}
catch (BarcodeScanException ex) when (ex.StatusName == "license_required")
{
    Console.WriteLine($"Missing license features: {ex.MissingLicenseFeatures}");
}
```

## Pricing And Commercial Use

`QualitySoft.Barcode` is proprietary commercial software owned by QS QualitySoft
GmbH. Evaluation and production use require a valid license agreement unless
your agreement explicitly grants demo usage.

Pricing, licensing options and product documentation are maintained on the
official product page:

`https://qualitysoft.de/products/qs-barcode-sdk/`

## Repository Scope

This public repository is intended for .NET SDK consumers. It contains:

- the managed .NET wrapper
- public API definitions
- package metadata
- documentation
- native asset layout placeholders

It does not contain:

- proprietary native engine source code
- private build infrastructure
- release signing material
- license files for customers
- internal packaging scripts

## Build The Wrapper

```powershell
dotnet build .\QualitySoft.Barcode.sln -c Release
```

Building the wrapper source is useful for inspection and integration work. The
official NuGet package is the supported distribution format for applications
because it includes the matching native runtime assets.

## License

See `LICENSE.md`. The SDK and native runtime are proprietary software owned by
QS QualitySoft GmbH. Third-party components remain subject to their own license
terms.

## Links

- NuGet package: `https://www.nuget.org/packages/QualitySoft.Barcode`
- Product page, pricing and documentation: `https://qualitysoft.de/products/qs-barcode-sdk/`
- Public repository: `https://github.com/QS-QualitySoft-GmbH/QSBarcode.Net`
- Changelog: `CHANGELOG.md`
