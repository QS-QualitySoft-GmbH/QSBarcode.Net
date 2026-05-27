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
dotnet add package QualitySoft.Barcode
```

Or add a `PackageReference` manually:

```xml
<PackageReference Include="QualitySoft.Barcode" Version="0.1.4" />
```

Visual Studio Package Manager Console:

```powershell
Install-Package QualitySoft.Barcode
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

## Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using QualitySoft.Barcode;

services.AddQualitySoftBarcode();
```

Then inject `IBarcodeReader` into services, controllers or background workers.

## License And Feature Checks

The SDK exposes the native license state so applications can enable or disable
features before starting a scan.

```csharp
var status = BarcodeLicense.GetStatus();

Console.WriteLine(status.IsDemo ? "Demo" : "Licensed");
Console.WriteLine($"Linear: {status.AllowsLinear}");
Console.WriteLine($"PDF417: {status.AllowsPdf417}");
Console.WriteLine($"DataMatrix: {status.AllowsDataMatrix}");
Console.WriteLine($"QR: {status.AllowsQr}");
Console.WriteLine($"Aztec: {status.AllowsAztec}");
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
