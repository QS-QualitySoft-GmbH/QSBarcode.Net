# QualitySoft.Barcode Changelog

## 0.2.3

- Rebuilt native runtime assets for `win-x86`, `win-x64`, `win-arm64`,
  `linux-x64`, `linux-arm64`, `osx-x64` and `osx-arm64`.
- Hardened EC/PDF417 native decoding against invalid candidates,
  out-of-range codewords and unsafe miss paths.
- Fixed a crash where scanning a Data Matrix-heavy TIFF with a PDF417 mask
  could terminate the process instead of returning zero PDF417 results.
- Added a regression fixture for the PDF417 miss path.
- Updated the .NET demo package reference to `QualitySoft.Barcode` `0.2.3`.

## 0.2.0

- First public NuGet-ready .NET SDK package line.
- Added managed sync and async reader APIs, dependency injection support,
  license status helpers, text encoding control and bundled native runtime
  assets for Windows, Linux and macOS.
