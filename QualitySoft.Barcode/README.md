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

```powershell
dotnet add package QualitySoft.Barcode
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

Async scans run the native work on a background task. Cancellation is observed
before the native scan starts and while stream data is copied.

## Scan Options

`BarcodeReaderOptions` defaults to native SDK behavior where possible. Set only
the values you need.

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

Useful defaults:

- `Symbologies = BarcodeSymbology.NativeDefault` passes mask `0` to the native
  engine.
- `MinLength = 1` follows the legacy loader default.
- Numeric tuning values default to `0`, which keeps native automatic behavior.
- `PageStart = -1` and `PageCount = 0` scan all pages.

## Symbologies

The wrapper exposes strongly typed masks for common QS Barcode features:

- Linear barcodes such as Code 128, Code 39, EAN/UPC and related 1D formats
- PDF417
- Data Matrix
- QR Code
- Aztec

License status determines which optional feature groups are available at
runtime.

## License Handling

The package includes a proprietary QS Barcode SDK license notice. Use of the SDK
requires a valid commercial license agreement with QS QualitySoft GmbH unless
your agreement explicitly permits demo or evaluation use.

The native engine exposes license status to managed code:

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

To test a specific license file:

```csharp
var status = BarcodeLicense.GetStatus(@"C:\licenses\qsbc.lic");
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

`DllNotFoundException` usually means the app was published without a supported
RID, the native assets were removed from the output, or the target platform is
not one of the supported runtime identifiers.

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

- NuGet: `QualitySoft.Barcode`
- Product page, pricing and documentation: `https://qualitysoft.de/products/qs-barcode-sdk/`
- Public .NET SDK repository: `https://github.com/QS-QualitySoft-GmbH/QSBarcode.Net`
