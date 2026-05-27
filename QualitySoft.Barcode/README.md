# QualitySoft.Barcode

Production-ready .NET wrapper for the native QS Barcode SDK.

`QualitySoft.Barcode` reads 1D and 2D barcodes from images and PDFs with a
small managed API, async overloads, dependency injection support, native scan
options, text encoding control and license-aware feature checks.

## Highlights

- Barcode reading from files, `FileInfo`, `Stream` and `byte[]`
- Sync and async APIs
- PDF rendering through bundled PDFium runtime assets
- Windows, Linux and macOS native binaries in one NuGet package
- License status helpers for demo and commercial feature gating
- Strongly typed scan options for symbology masks, page ranges, DPI, threshold,
  orientation and legacy tuning values
- Text decoding with UTF-8 fallback handling and configurable legacy encodings
- ASP.NET Core / generic host dependency injection integration

## Installation

NuGet package:
`https://www.nuget.org/packages/QualitySoft.Barcode`

Install with the .NET CLI:

```powershell
dotnet add package QualitySoft.Barcode --version 0.2.3
```

Or add a `PackageReference` manually:

```xml
<PackageReference Include="QualitySoft.Barcode" Version="0.2.3" />
```

Visual Studio Package Manager Console:

```powershell
Install-Package QualitySoft.Barcode -Version 0.2.3
```

## Supported Frameworks

The package targets:

- `.NET Framework 4.6.2`
- `.NET Standard 2.0`
- `.NET Standard 2.1`
- `.NET Core 3.1`
- `.NET 6`
- `.NET 8`

`netstandard2.0` is included for broad compatibility with older .NET Framework
and .NET Core applications. New applications should usually use .NET 6 or newer.

## Supported Platforms

The NuGet package contains native runtime assets for:

| RID | OS | CPU |
| --- | --- | --- |
| `win-x86` | Windows | x86 |
| `win-x64` | Windows | x64 |
| `win-arm64` | Windows | ARM64 |
| `linux-x64` | Linux glibc | x64 |
| `linux-arm64` | Linux glibc | ARM64 / AWS Graviton |
| `osx-x64` | macOS | Intel |
| `osx-arm64` | macOS | Apple Silicon |

Mobile platforms such as Android and iOS are intentionally not part of this
package. They require separate .NET MAUI or platform binding packages.

PDF input is supported on all listed desktop/server RIDs through bundled PDFium
runtime assets. Applications normally do not need to copy native libraries
manually when they restore or publish for one of the supported runtime
identifiers.

## Quick Start

```csharp
using QualitySoft.Barcode;

IBarcodeReader reader = new QualitySoftBarcodeReader();

var results = await reader.ReadAsync(
    @"C:\data\label.pdf",
    new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.Code128 | BarcodeSymbology.DataMatrix,
        MinLength = 4,
        Dpi = 300,
        PageStart = 0,
        PageCount = 1
    });

foreach (var result in results)
{
    Console.WriteLine($"{result.Symbology}: {result.Text}");
}
```

## Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using QualitySoft.Barcode;

var services = new ServiceCollection();
services.AddQualitySoftBarcode();

using var provider = services.BuildServiceProvider();
var reader = provider.GetRequiredService<IBarcodeReader>();
```

`AddQualitySoftBarcode()` registers `IBarcodeReader` as a singleton using
`TryAddSingleton`, so applications can replace it with their own implementation
before calling the extension method.

Applications can also register managed default scan options once. Per-call
options still override these defaults.

```csharp
services.AddQualitySoftBarcode(options =>
{
    options.Symbologies = BarcodeSymbology.LinearMask;
    options.MinLength = 4;
    options.Dpi = 300;
    options.TextEncoding = Encoding.GetEncoding(1252);
});
```

For services that process many files concurrently, configure reader-level
settings explicitly:

```csharp
services.AddQualitySoftBarcode(new BarcodeReaderSettings
{
    MaxConcurrentScans = Environment.ProcessorCount,
    NativeScanThreadStackSize = BarcodeReaderSettings.DefaultNativeScanThreadStackSize,
    DefaultOptions = new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.Default,
        MinLength = 4,
        Dpi = 300
    }
});
```

## Reading From Different Sources

```csharp
IReadOnlyList<BarcodeResult> fromPath = reader.Read(@"C:\data\label.tif");
IReadOnlyList<BarcodeResult> fromBytes = reader.Read(File.ReadAllBytes(@"C:\data\label.png"));

await using var stream = File.OpenRead(@"C:\data\document.pdf");
IReadOnlyList<BarcodeResult> fromStream = await reader.ReadAsync(stream);
```

The same source types are available in both sync and async form:

- `string`
- `FileInfo`
- `byte[]`
- `Stream`

Async scans take a snapshot of the supplied `BarcodeReaderOptions` before the
native work starts. Cancellation is observed before the native scan starts and
while stream data is copied. Once native scanning is running, the native call is
allowed to finish on its background scan thread; the returned task can still be
canceled so application request handling does not have to wait for the native
call to return.

`QualitySoftBarcodeReader` is safe to register as a singleton. It limits the
number of concurrently executing native scans per reader instance using
`BarcodeReaderSettings.MaxConcurrentScans`. This prevents unbounded dedicated
native scan threads and their stack reservations under load. `ReadAsync(byte[])`
copies the supplied byte array before queuing native work so caller-side buffer
reuse cannot corrupt an in-flight scan.

## Scan Options

`BarcodeReaderOptions` defaults to native SDK behavior where possible. Set only
the values you need. Numeric tuning values use `0` as "native default" unless
the option documents a different default.

```csharp
var options = new BarcodeReaderOptions
{
    Symbologies = BarcodeSymbology.LinearMask,
    MinLength = 4,
    Dpi = 300,
    Threshold = 0,
    PageStart = -1,
    PageCount = 0
};
```

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
| `DataMatrixFinderAngleTolerance` | `0` | Data Matrix finder angle tuning. |
| `DataMatrixOverlapPercent` | `0` | Data Matrix overlap tuning. |
| `DataMatrixMaxLineCandidates` | `0` | Data Matrix candidate limit. |
| `MaxSkewDegrees` | `0` | Maximum skew correction in degrees. |
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

## Symbologies

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
`Postal` families.

License status determines which optional feature groups are available at
runtime. Applications can check `BarcodeLicense.GetStatus()` before enabling
feature-specific workflows.

## License Handling

The package includes a proprietary QS Barcode SDK license notice. Use of the SDK
requires a valid commercial license agreement with QS QualitySoft GmbH unless
your agreement explicitly permits demo or evaluation use.

The native engine exposes license status to managed code:

By default, the native runtime searches for `qsbc.lic`. For Linux, macOS,
containers and hosted applications, set `QSBC_LICENSE_FILE` to an absolute
license path before the first scan:

```bash
export QSBC_LICENSE_FILE=/etc/qualitysoft/qsbc.lic
```

```csharp
using QualitySoft.Barcode;

var status = BarcodeLicense.GetStatus();

Console.WriteLine(status.IsDemo ? "Demo license" : "Licensed");
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

To test a specific license file:

```csharp
var status = BarcodeLicense.GetStatus("/etc/qualitysoft/qsbc.lic");
```

Before enabling feature-specific UI or workflows, check the requested
symbologies:

```csharp
var requested = BarcodeSymbology.DataMatrix | BarcodeSymbology.Qr;
var status = BarcodeLicense.GetStatus();

if (!status.CanScan(requested))
{
    var missing = string.Join(", ", status.MissingFeatureListFor(requested));
    Console.WriteLine($"Missing license features: {missing}");
}
```

If a scan requests only unlicensed symbologies, the native SDK returns
`license_required` and the wrapper throws `BarcodeScanException`. The exception
includes:

- `RequestedSymbologies`
- `LicenseStatus`
- `MissingLicenseFeatures`
- native `StatusCode` and `StatusName`

```csharp
try
{
    await reader.ReadAsync(path, new BarcodeReaderOptions
    {
        Symbologies = BarcodeSymbology.DataMatrix
    });
}
catch (BarcodeScanException ex) when (ex.StatusName == "license_required")
{
    Console.WriteLine($"License required: {ex.MissingLicenseFeatures}");
}
```

## Text Encoding

`BarcodeResult.Bytes` always contains the original decoded payload bytes.
`BarcodeResult.Text` is decoded as UTF-8 when valid, otherwise with the legacy
fallback encoding. You can set a specific encoding when needed:

```csharp
using System.Text;
using QualitySoft.Barcode;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var results = reader.Read(path, new BarcodeReaderOptions
{
    TextEncoding = Encoding.GetEncoding(1252)
});
```

## Result Data

Each `BarcodeResult` contains:

- decoded text
- original payload bytes
- symbology
- image format
- image/page index
- source image dimensions
- optional barcode bounds

```csharp
foreach (var result in results)
{
    Console.WriteLine(result.Text);
    Console.WriteLine(result.Symbology);
    Console.WriteLine(result.Bounds);
}
```

## Native Runtime Assets

The package includes the native QS Barcode loader and PDFium runtime per
supported RID:

```text
runtimes/win-x86/native/
runtimes/win-x64/native/
runtimes/win-arm64/native/
runtimes/linux-x64/native/
runtimes/linux-arm64/native/
runtimes/osx-x64/native/
runtimes/osx-arm64/native/
```

.NET resolves the matching native library automatically when the application is
published or restored for a supported runtime identifier.

For deployment health checks, call the native runtime diagnostic API at startup:

```csharp
if (!BarcodeNativeLibrary.TryGetVersion(out var version, out var error))
{
    Console.WriteLine(error);
}
else
{
    Console.WriteLine($"QS Barcode native runtime: {version}");
}
```

`BarcodeNativeLibrary.GetDiagnostics()` returns the current runtime identifier,
expected native library file name and probing locations. For custom deployment
layouts, set `QSBC_NATIVE_LIBRARY` to an absolute path to the native loader.

## Release Notes

### 0.2.3

- Rebuilt native runtime assets for Windows, Linux and macOS.
- Hardened EC/PDF417 native decoding against invalid candidates and
  out-of-range codewords.
- Fixed a PDF417 miss case where scanning a Data Matrix-heavy TIFF with a
  PDF417 mask could terminate the native process instead of returning zero
  results.
- Added a regression fixture for the PDF417 miss path.

## Publish Examples

Framework-dependent Linux x64 publish:

```powershell
dotnet publish -c Release -r linux-x64 --self-contained false
```

Windows x64 publish:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

AWS Graviton / Linux ARM64 publish:

```powershell
dotnet publish -c Release -r linux-arm64 --self-contained false
```

## Troubleshooting

`BarcodeNativeLibraryException` usually means the app was published without a
supported RID, the native assets were removed from the output, or the target
platform is not one of the supported runtime identifiers. The exception message
includes the runtime identifier and native probing locations.

For Linux deployments, use glibc-based distributions for this package. Alpine
Linux/musl is not included in the current package.

If PDF input cannot be rendered, verify that the matching `pdfium` native asset
is present in the publish output beside the QS Barcode loader library.

## License

This package is proprietary software owned by QS QualitySoft GmbH. The NuGet
package contains `LICENSE.md` with the full proprietary license notice. Any
production or commercial use requires a separate written license agreement with
QS QualitySoft GmbH.

Pricing, licensing options and product documentation are maintained on the
official product page:

`https://qualitysoft.de/products/qs-barcode-sdk/`

Third-party components such as PDFium remain subject to their own license terms.

## Links

- NuGet package: `https://www.nuget.org/packages/QualitySoft.Barcode`
- Product page, pricing and documentation: `https://qualitysoft.de/products/qs-barcode-sdk/`
- Public .NET SDK repository: `https://github.com/QS-QualitySoft-GmbH/QSBarcode.Net`
- Changelog: `https://github.com/QS-QualitySoft-GmbH/QSBarcode.Net/blob/main/CHANGELOG.md`
