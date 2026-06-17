using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace QualitySoft.Barcode.Tests;

public sealed class NativeSmokeTests
{
    private readonly ITestOutputHelper _output;

    public NativeSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void NativeRuntime_Loads_WhenAvailable()
    {
        if (!TryRequireNativeRuntime())
        {
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(BarcodeNativeLibrary.GetVersion()));
        Assert.False(string.IsNullOrWhiteSpace(BarcodeNativeLibrary.GetEngineVersion()));
        Assert.True(BarcodeNativeLibrary.GetVersionMajor() > 0);
        Assert.NotEqual(BarcodeNativeCapabilities.None, BarcodeNativeLibrary.GetCapabilities());
        Assert.True(BarcodeNativeLibrary.IsFormatSupported(BarcodeImageFormat.Pdf));
        Assert.False(string.IsNullOrWhiteSpace(BarcodeNativeLibrary.GetFormatName(BarcodeImageFormat.Pdf)));
        Assert.False(string.IsNullOrWhiteSpace(BarcodeNativeLibrary.GetStatusName(0)));
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void RenderPage_RendersPdfFixture_WhenNativeRuntimeAndFixtureAreAvailable()
    {
        if (!TryRequireNativeRuntime() || !TryFindFixture("AdobeTest.pdf", out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var image = reader.RenderPage(fixture, CreateSmokeOptions());

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.Equal(BarcodeImageFormat.Pdf, image.SourceFormat);
        Assert.Equal(0, image.PageIndex);
        Assert.Equal(BarcodeRenderedPixelFormat.Bmp24, image.PixelFormat);
        Assert.True(image.BmpBytes.Length > 2);
        Assert.Equal((byte)'B', image.BmpBytes[0]);
        Assert.Equal((byte)'M', image.BmpBytes[1]);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void PageCount_ReturnsPdfFixturePages_WhenNativeRuntimeAndFixtureAreAvailable()
    {
        if (!TryRequireNativeRuntime() || !TryFindFixture("AdobeTest.pdf", out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var fileCount = reader.GetPageCount(fixture);
        var bytes = File.ReadAllBytes(fixture);
        var memoryCount = reader.GetPageCount(bytes);

        Assert.True(fileCount >= 1);
        Assert.Equal(fileCount, memoryCount);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void Read_FileAndPointer_ReturnMatchingResults_WhenLicensedFixtureIsAvailable()
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindDecodableFixture(out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var fileResults = reader.Read(fixture, options);
        var bytes = File.ReadAllBytes(fixture);
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        try
        {
            var pointerResults = reader.Read(handle.AddrOfPinnedObject(), bytes.Length, options);

            Assert.NotEmpty(fileResults);
            Assert.NotEmpty(pointerResults);
            Assert.Equal(fileResults.Select(result => result.Text), pointerResults.Select(result => result.Text));
        }
        finally
        {
            handle.Free();
        }
    }

    public static IEnumerable<object[]> Demo2DSymbologyCases()
    {
        yield return new object[] { "DataMatrix", BarcodeSymbology.DataMatrix, "BarDM.tif" };
        yield return new object[] { "QR", BarcodeSymbology.Qr, "QR_Codes.jpg" };
        yield return new object[] { "Aztec", BarcodeSymbology.Aztec, "aztec.gif" };
        yield return new object[] { "PDF417", BarcodeSymbology.Pdf417, "BarPDF.tif" };
    }

    [Theory]
    [MemberData(nameof(Demo2DSymbologyCases))]
    [Trait("Category", "NativeSmoke")]
    public void Read_2DFixtureWithoutLicense_ReturnsCorruptedDemoPayload(string symbologyName, BarcodeSymbology symbology, string fixtureName)
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindFixture(fixtureName, out var fixture))
        {
            return;
        }

        var options = CreateSmokeOptions();
        options.Symbologies = symbology;

        var expectedTexts = Array.Empty<string>();
        if (BarcodeLicense.GetStatus().CanScan(symbology))
        {
            using var licensedReader = CreateSmokeReader();
            expectedTexts = licensedReader.Read(fixture, options)
                    .Select(result => result.Text)
                    .Where(text => !string.IsNullOrEmpty(text))
                    .ToArray();

            Assert.NotEmpty(expectedTexts);
        }

        var exitCode = RunDemoLicenseProbe(symbologyName, fixture, expectedTexts);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void ReadRawGray8_DecodesRenderedFixture_WhenLicensedFixtureIsAvailable()
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindDecodableFixture(out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var gray = reader.RenderPageGray8(fixture, options);
        var results = reader.ReadRawGray8(gray.Bytes, checked((int)gray.Width), checked((int)gray.Height), checked((int)gray.Stride), options);

        Assert.NotEmpty(results);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void RenderPageGray8_AndMemoryRender_ReturnValidPixels_WhenNativeRuntimeAndFixtureAreAvailable()
    {
        if (!TryRequireNativeRuntime() || !TryFindFixture("AdobeTest.pdf", out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var fileGray = reader.RenderPageGray8(fixture, options);
        var memoryBmp = reader.RenderPage(File.ReadAllBytes(fixture), options);

        Assert.Equal(BarcodeRenderedPixelFormat.Gray8, fileGray.PixelFormat);
        Assert.Equal(fileGray.Width, fileGray.Stride);
        Assert.Equal(checked((int)(fileGray.Width * fileGray.Height)), fileGray.Bytes.Length);
        AssertValidBmp(memoryBmp);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public void ReadRawPixels_DecodesRgbPixels_WhenLicensedFixtureIsAvailable()
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindDecodableFixture(out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var gray = reader.RenderPageGray8(fixture, options);
        var rgb = ExpandGray8ToRgb24(gray.Bytes);
        var results = reader.ReadRawPixels(rgb, checked((int)gray.Width), checked((int)gray.Height), BarcodeRawPixelFormat.Rgb24, checked((int)gray.Width * 3), options);

        Assert.NotEmpty(results);
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public async Task Read_ParallelFileMemoryAndPointerScans_ReturnStableResults_WhenLicensedFixtureIsAvailable()
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindDecodableFixture(out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var bytes = File.ReadAllBytes(fixture);
        var expected = reader.Read(fixture, options).Select(result => result.Text).ToArray();
        Assert.NotEmpty(expected);

        var tasks = Enumerable.Range(0, 16)
            .Select(index => Task.Run(() => ReadFixtureVariant(reader, fixture, bytes, options, index)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result =>
        {
            Assert.NotEmpty(result);
            Assert.Equal(expected, result.Select(barcode => barcode.Text));
        });
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public async Task ReadAsync_ParallelMemoryScans_ReturnStableResults_WhenLicensedFixtureIsAvailable()
    {
        if (!TryRequireLicensedNativeRuntime() || !TryFindDecodableFixture(out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var bytes = File.ReadAllBytes(fixture);
        var expected = reader.Read(bytes, options).Select(result => result.Text).ToArray();
        Assert.NotEmpty(expected);

        var tasks = Enumerable.Range(0, 16)
            .Select(_ => reader.ReadAsync(bytes, options))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result =>
        {
            Assert.NotEmpty(result);
            Assert.Equal(expected, result.Select(barcode => barcode.Text));
        });
    }

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public async Task RenderPage_ParallelPdfRenders_ReturnValidBmps_WhenNativeRuntimeAndFixtureAreAvailable()
    {
        if (!TryRequireNativeRuntime() || !TryFindFixture("AdobeTest.pdf", out var fixture))
        {
            return;
        }

        using var reader = CreateSmokeReader();
        var options = CreateSmokeOptions();
        var tasks = Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => reader.RenderPage(fixture, options)))
            .ToArray();

        var images = await Task.WhenAll(tasks);

        Assert.All(images, AssertValidBmp);
        Assert.All(images, image => Assert.Equal(BarcodeImageFormat.Pdf, image.SourceFormat));
    }

    private bool TryRequireNativeRuntime()
    {
        if (BarcodeNativeLibrary.TryGetVersion(out _, out var error))
        {
            return true;
        }

        _output.WriteLine("Native smoke test skipped: " + error);
        return false;
    }

    private bool TryRequireLicensedNativeRuntime()
    {
        if (!TryRequireNativeRuntime())
        {
            return false;
        }

        var licenseFile = FindLicenseFile();
        if (licenseFile == null)
        {
            _output.WriteLine("Native smoke test skipped: qsbc.lic not found. Put it next to the test project or set QSBC_LICENSE_FILE.");
            return false;
        }

        Environment.SetEnvironmentVariable("QSBC_LICENSE_FILE", licenseFile);

        if (!BarcodeLicense.TryGetStatus(licenseFile, out var status, out var error))
        {
            _output.WriteLine("Native smoke test skipped: " + error);
            return false;
        }

        if (status.Features == BarcodeLicenseFeatures.None)
        {
            _output.WriteLine("Native smoke test skipped: license has no scan features.");
            return false;
        }

        return true;
    }

    private static QualitySoftBarcodeReader CreateSmokeReader()
    {
        return new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
            DefaultOptions = CreateSmokeOptions()
        });
    }

    private static BarcodeReaderOptions CreateSmokeOptions()
    {
        return new BarcodeReaderOptions
        {
            PageStart = 0,
            PageCount = 1,
            Dpi = 150,
            MinLength = 1,
            ScanTimeoutMs = 15_000
        };
    }

    private bool TryFindDecodableFixture(out string fixture)
    {
        foreach (var candidate in new[]
        {
            "QR_Codes.jpg",
            "BarDM.tif",
            "linear_dm.tif",
            "labels128_dm.jpg",
            "aztec.gif",
            "AdobeTest.pdf"
        })
        {
            if (TryFindFixture(candidate, out fixture))
            {
                return true;
            }
        }

        fixture = string.Empty;
        _output.WriteLine("Native smoke test skipped: no barcode fixture was found.");
        return false;
    }

    private bool TryFindFixture(string fileName, out string path)
    {
        foreach (var fixtureRoot in GetFixtureRoots())
        {
            var candidate = Path.Combine(fixtureRoot, fileName);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        _output.WriteLine("Native smoke test skipped: fixture not found: " + fileName);
        return false;
    }

    private static IReadOnlyList<BarcodeResult> ReadFixtureVariant(
        QualitySoftBarcodeReader reader,
        string fixture,
        byte[] bytes,
        BarcodeReaderOptions options,
        int index)
    {
        if (index % 3 == 0)
        {
            return reader.Read(fixture, options);
        }

        if (index % 3 == 1)
        {
            return reader.Read(bytes, options);
        }

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return reader.Read(handle.AddrOfPinnedObject(), bytes.Length, options);
        }
        finally
        {
            handle.Free();
        }
    }

    private static void AssertValidBmp(BarcodeRenderedImage image)
    {
        Assert.Equal(BarcodeRenderedPixelFormat.Bmp24, image.PixelFormat);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.True(image.BmpBytes.Length > 2);
        Assert.Equal((byte)'B', image.BmpBytes[0]);
        Assert.Equal((byte)'M', image.BmpBytes[1]);
    }

    private static string? FindLicenseFile()
    {
        var configured = Environment.GetEnvironmentVariable("QSBC_LICENSE_FILE");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return Path.GetFullPath(configured);
        }

        foreach (var root in GetSearchRoots())
        {
            var candidate = Path.Combine(root, "qsbc.lic");
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private int RunDemoLicenseProbe(string symbologyName, string fixture, IReadOnlyCollection<string> expectedTexts)
    {
        var host = FindTestHost();
        if (host == null)
        {
            _output.WriteLine("Native smoke test skipped: demo license probe host was not found.");
            return 0;
        }

        var missingLicenseDirectory = Path.Combine(Path.GetTempPath(), "qsbc-missing-" + Guid.NewGuid().ToString("N"));
        var missingLicenseFile = Path.Combine(missingLicenseDirectory, "qsbc.lic");
        var workingDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "qsbc-demo-probe-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add(host);
            startInfo.ArgumentList.Add("demo-license-probe");
            startInfo.ArgumentList.Add(symbologyName);
            startInfo.ArgumentList.Add(fixture);
            foreach (var expectedText in expectedTexts)
            {
                startInfo.ArgumentList.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedText)));
            }

            startInfo.Environment["QSBC_LICENSE_FILE"] = missingLicenseFile;

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start demo license probe host.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            _output.WriteLine(stdout);
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _output.WriteLine(stderr);
            }

            return process.ExitCode;
        }
        finally
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string? FindTestHost()
    {
        foreach (var root in GetSearchRoots())
        {
            var candidate = Path.Combine(root, "QualitySoft.Barcode.TestHost.dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(root, "sdk", "dotnet", "QualitySoft.Barcode.TestHost", "bin", "Release", "net8.0", "QualitySoft.Barcode.TestHost.dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(root, "sdk", "dotnet", "QualitySoft.Barcode.TestHost", "bin", "Debug", "net8.0", "QualitySoft.Barcode.TestHost.dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetFixtureRoots()
    {
        return GetSearchRoots().Select(root => Path.Combine(root, "fixtures")).Where(Directory.Exists);
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (seen.Add(directory.FullName))
            {
                yield return directory.FullName;
            }

            directory = directory.Parent;
        }

        directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            if (seen.Add(directory.FullName))
            {
                yield return directory.FullName;
            }

            directory = directory.Parent;
        }
    }

    private static GrayImage DecodeBmp24ToGray8(byte[] bmp)
    {
        if (bmp.Length < 54 || bmp[0] != (byte)'B' || bmp[1] != (byte)'M')
        {
            throw new InvalidOperationException("Rendered image is not a BMP file.");
        }

        var pixelOffset = BitConverter.ToInt32(bmp, 10);
        var dibHeaderSize = BitConverter.ToInt32(bmp, 14);
        var width = BitConverter.ToInt32(bmp, 18);
        var signedHeight = BitConverter.ToInt32(bmp, 22);
        var bitsPerPixel = BitConverter.ToUInt16(bmp, 28);
        var compression = BitConverter.ToInt32(bmp, 30);

        if (dibHeaderSize < 40 || width <= 0 || signedHeight == 0 || bitsPerPixel != 24 || compression != 0)
        {
            throw new InvalidOperationException("Rendered BMP format is not supported by the test decoder.");
        }

        var height = Math.Abs(signedHeight);
        var sourceStride = ((width * 3 + 3) / 4) * 4;
        var pixels = new byte[checked(width * height)];
        var topDown = signedHeight < 0;

        for (var y = 0; y < height; y++)
        {
            var sourceY = topDown ? y : height - 1 - y;
            var sourceRow = pixelOffset + sourceY * sourceStride;
            var targetRow = y * width;

            for (var x = 0; x < width; x++)
            {
                var source = sourceRow + x * 3;
                var b = bmp[source];
                var g = bmp[source + 1];
                var r = bmp[source + 2];
                pixels[targetRow + x] = (byte)((r * 299 + g * 587 + b * 114 + 500) / 1000);
            }
        }

        return new GrayImage(pixels, width, height);
    }

    private static byte[] ExpandGray8ToRgb24(byte[] gray)
    {
        var rgb = new byte[checked(gray.Length * 3)];
        for (var i = 0; i < gray.Length; i++)
        {
            var offset = i * 3;
            rgb[offset] = gray[i];
            rgb[offset + 1] = gray[i];
            rgb[offset + 2] = gray[i];
        }

        return rgb;
    }

    private readonly record struct GrayImage(byte[] Pixels, int Width, int Height);
}
