# Native Runtime Assets

Place native QS Barcode SDK binaries here before creating a release NuGet
package.

Expected layout:

```text
native/
  win-x86/qs_barcode_loader_sdk.dll
  win-x86/pdfium.dll
  win-x86/qs_barcode_pdf_render_worker.exe
  win-x64/qs_barcode_loader_sdk.dll
  win-x64/pdfium.dll
  win-x64/qs_barcode_pdf_render_worker.exe
  win-arm64/qs_barcode_loader_sdk.dll
  win-arm64/pdfium.dll
  win-arm64/qs_barcode_pdf_render_worker.exe
  linux-x64/libqs_barcode_loader_sdk.so
  linux-x64/libpdfium.so
  linux-x64/qs_barcode_pdf_render_worker
  linux-arm64/libqs_barcode_loader_sdk.so
  linux-arm64/libpdfium.so
  linux-arm64/qs_barcode_pdf_render_worker
  osx-x64/libqs_barcode_loader_sdk.dylib
  osx-x64/libpdfium.dylib
  osx-x64/qs_barcode_pdf_render_worker
  osx-arm64/libqs_barcode_loader_sdk.dylib
  osx-arm64/libpdfium.dylib
  osx-arm64/qs_barcode_pdf_render_worker
```

During local Windows development, `dotnet pack` also picks up
`zig-out/bin/qs_barcode_loader_sdk.dll`, `zig-out/bin/pdfium.dll` and
`zig-out/bin/qs_barcode_pdf_render_worker.exe` as `runtimes/win-x64/native/`
assets when they exist.
