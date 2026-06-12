using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using QualitySoft.Barcode;

BenchmarkRunner.Run<BarcodeReaderBenchmarks>();

[MemoryDiagnoser]
public class BarcodeReaderBenchmarks
{
    private QualitySoftBarcodeReader _reader = null!;
    private QualitySoftBarcodeReader _unsafeAsyncReader = null!;
    private string _fixture = string.Empty;
    private string _pdfFixture = string.Empty;
    private byte[] _fixtureBytes = Array.Empty<byte>();
    private byte[] _grayPixels = Array.Empty<byte>();
    private int _grayWidth;
    private int _grayHeight;
    private BarcodeReaderOptions _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        ApplyLocalLicense();

        _fixture = FindFixture("QR_Codes.jpg", "BarDM.tif", "linear_dm.tif", "labels128_dm.jpg");
        _pdfFixture = FindFixture("AdobeTest.pdf");
        _fixtureBytes = File.ReadAllBytes(_fixture);
        _options = new BarcodeReaderOptions
        {
            PageStart = 0,
            PageCount = 1,
            Dpi = 150,
            MinLength = 1,
            ScanTimeoutMs = 15_000
        };

        _reader = new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
            MaxConcurrentScans = 4,
            PdfRenderWorkerWarmupCount = 4,
            DefaultOptions = _options
        });
        _unsafeAsyncReader = new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
            MaxConcurrentScans = 4,
            PdfRenderWorkerWarmupCount = 4,
            CopyInputBuffersForAsyncByteArray = false,
            DefaultOptions = _options
        });

        var rendered = _reader.RenderPageGray8(_fixture, _options);
        _grayPixels = rendered.Bytes;
        _grayWidth = checked((int)rendered.Width);
        _grayHeight = checked((int)rendered.Height);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _reader.Dispose();
        _unsafeAsyncReader.Dispose();
    }

    [Benchmark]
    public IReadOnlyList<BarcodeResult> ReadFile()
    {
        return _reader.Read(_fixture, _options);
    }

    [Benchmark]
    public IReadOnlyList<BarcodeResult> ReadByteArray()
    {
        return _reader.Read(_fixtureBytes, _options);
    }

    [Benchmark]
    public async Task<IReadOnlyList<BarcodeResult>> ReadByteArrayAsyncWithDefensiveCopy()
    {
        return await _reader.ReadAsync(_fixtureBytes, _options);
    }

    [Benchmark]
    public async Task<IReadOnlyList<BarcodeResult>> ReadByteArrayAsyncWithoutDefensiveCopy()
    {
        return await _unsafeAsyncReader.ReadAsync(_fixtureBytes, _options);
    }

    [Benchmark]
    public IReadOnlyList<BarcodeResult> ReadPointer()
    {
        var handle = GCHandle.Alloc(_fixtureBytes, GCHandleType.Pinned);
        try
        {
            return _reader.Read(handle.AddrOfPinnedObject(), _fixtureBytes.Length, _options);
        }
        finally
        {
            handle.Free();
        }
    }

    [Benchmark]
    public IReadOnlyList<BarcodeResult> ReadRawGray8()
    {
        return _reader.ReadRawGray8(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _options);
    }

    [Benchmark]
    public BarcodeRenderedImage RenderPdfPageGray8()
    {
        return _reader.RenderPageGray8(_pdfFixture, _options);
    }

    [Benchmark]
    public async Task ParallelPdfRender()
    {
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => _reader.RenderPageGray8(_pdfFixture, _options)))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private static void ApplyLocalLicense()
    {
        var configured = Environment.GetEnvironmentVariable("QSBC_LICENSE_FILE");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return;
        }

        foreach (var root in SearchRoots())
        {
            var candidate = Path.Combine(root, "qsbc.lic");
            if (File.Exists(candidate))
            {
                Environment.SetEnvironmentVariable("QSBC_LICENSE_FILE", Path.GetFullPath(candidate));
                return;
            }
        }
    }

    private static string FindFixture(params string[] names)
    {
        foreach (var root in SearchRoots())
        {
            var fixtures = Path.Combine(root, "fixtures");
            foreach (var name in names)
            {
                var candidate = Path.Combine(fixtures, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException("Benchmark fixture not found. Copy fixtures into the benchmark output or run from the repository.");
    }

    private static IEnumerable<string> SearchRoots()
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
}
