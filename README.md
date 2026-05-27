# QualitySoft.Barcode .NET SDK

The .NET SDK wraps the native QS Barcode loader and exposes barcode reading,
license checks, scan options and dependency injection support in the
`QualitySoft.Barcode` NuGet package.

## Projects

- `QualitySoft.Barcode/` - package source for `QualitySoft.Barcode`.
- `QualitySoft.Barcode.sln` - Visual Studio solution for the .NET SDK.
- `native/` - local native runtime assets used when packing the NuGet package.

## Build

```powershell
dotnet build .\sdk\dotnet\QualitySoft.Barcode.sln -c Release
```

## Pack And Publish

Use the SDK-root scripts:

```powershell
$Version = "0.1.4"

powershell -ExecutionPolicy Bypass -File .\sdk\build-artifacts.ps1 -Version $Version
powershell -ExecutionPolicy Bypass -File .\sdk\publish-nuget.ps1 -Version $Version -ApiKey $env:NUGET_API_KEY
```

See `sdk/PACKAGING.md` for the full local packaging workflow.
