using System.Runtime.InteropServices;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using QualitySoft.Barcode;

if (args.Contains("--compare-raw", StringComparer.OrdinalIgnoreCase))
{
    RawComparison.Run();
    return;
}

BenchmarkRunner.Run<BarcodeReaderBenchmarks>(BenchmarkConfig.Create());

internal static class BenchmarkConfig
{
    public static IConfig Create()
    {
        var artifactsPath = Path.Combine(FindSdkDotnetRoot(), "BenchmarkDotNet.Artifacts");
        Directory.CreateDirectory(Path.Combine(artifactsPath, "results"));
        return ManualConfig.Create(DefaultConfig.Instance).WithArtifactsPath(artifactsPath);
    }

    private static string FindSdkDotnetRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "sdk", "dotnet");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            if (string.Equals(directory.Name, "dotnet", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(directory.Parent?.Name, "sdk", StringComparison.OrdinalIgnoreCase))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}

[MemoryDiagnoser]
public class BarcodeReaderBenchmarks
{
    private QualitySoftBarcodeReader _reader = null!;
    private string _fixture = string.Empty;
    private string _pdfFixture = string.Empty;
    private byte[] _fixtureBytes = Array.Empty<byte>();
    private byte[] _grayPixels = Array.Empty<byte>();
    private int _grayWidth;
    private int _grayHeight;
    private BarcodeReaderOptions _options = null!;
    private BarcodeReaderOptions _qrOptions = null!;

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
        _qrOptions = _options.Clone();
        _qrOptions.Symbologies = BarcodeSymbology.Qr;

        _reader = new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
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
    public async Task<IReadOnlyList<BarcodeResult>> ReadByteArrayAsync()
    {
        return await _reader.ReadAsync(_fixtureBytes, _options);
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
    public IReadOnlyList<BarcodeResult> ReadRawGray8Qr()
    {
        return _reader.ReadRawGray8(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _qrOptions);
    }

    [Benchmark]
    public int DirectNativeRawGray8CountOnly()
    {
        return DirectNative.scan_gray8_count_only(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _options);
    }

    [Benchmark]
    public int DirectNativeRawGray8CollectText()
    {
        return DirectNative.scan_gray8_collect_text(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _options);
    }

    [Benchmark]
    public int DirectNativeRawGray8QrCountOnly()
    {
        return DirectNative.scan_gray8_count_only(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _qrOptions);
    }

    [Benchmark]
    public int DirectNativeRawGray8QrCollectText()
    {
        return DirectNative.scan_gray8_collect_text(_grayPixels, _grayWidth, _grayHeight, _grayWidth, _qrOptions);
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

    internal static void ApplyLocalLicense()
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

    internal static string FindFixture(params string[] names)
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

    internal static IEnumerable<string> SearchRoots()
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

internal static class RawComparison
{
    public static void Run()
    {
        BarcodeReaderBenchmarks.ApplyLocalLicense();

        var fixture = BarcodeReaderBenchmarks.FindFixture("QR_Codes.jpg", "BarDM.tif", "linear_dm.tif", "labels128_dm.jpg");
        var options = new BarcodeReaderOptions
        {
            PageStart = 0,
            PageCount = 1,
            Dpi = 150,
            MinLength = 1,
            ScanTimeoutMs = 15_000
        };
        var qrOptions = options.Clone();
        qrOptions.Symbologies = BarcodeSymbology.Qr;

        using var reader = new QualitySoftBarcodeReader(new BarcodeReaderSettings
        {
            DefaultOptions = options
        });

        var rendered = reader.RenderPageGray8(fixture, options);
        var pixels = rendered.Bytes;
        var width = checked((int)rendered.Width);
        var height = checked((int)rendered.Height);
        var stride = checked((int)rendered.Stride);

        Console.WriteLine("fixture=" + fixture);
        Console.WriteLine("gray=" + width + "x" + height + " stride=" + stride + " bytes=" + pixels.Length);
        Console.WriteLine();

        Measure("wrapper raw NativeDefault", () => reader.ReadRawGray8(pixels, width, height, stride, options).Count);
        Measure("direct raw NativeDefault count-only", () => DirectNative.scan_gray8_count_only(pixels, width, height, stride, options));
        Measure("direct raw NativeDefault collect-text", () => DirectNative.scan_gray8_collect_text(pixels, width, height, stride, options));
        Measure("wrapper raw QR", () => reader.ReadRawGray8(pixels, width, height, stride, qrOptions).Count);
        Measure("direct raw QR count-only", () => DirectNative.scan_gray8_count_only(pixels, width, height, stride, qrOptions));
        Measure("direct raw QR collect-text", () => DirectNative.scan_gray8_collect_text(pixels, width, height, stride, qrOptions));
    }

    private static void Measure(string name, Func<int> action)
    {
        const int warmup = 1;
        const int iterations = 5;

        for (var i = 0; i < warmup; i++)
        {
            _ = action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var times = new double[iterations];
        var result = 0;
        for (var i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            result = action();
            stopwatch.Stop();
            times[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Console.WriteLine($"{name,-38} avg={times.Average(),8:F2} ms min={times.Min(),8:F2} ms max={times.Max(),8:F2} ms result={result}");
    }
}

internal static class DirectNative
{
    private static readonly NativeMethods.ResultCallback CountOnlyCallback = count_only_callback;
    private static readonly NativeMethods.ResultCallback CollectTextCallback = collect_text_callback;

    public static int scan_gray8_count_only(byte[] pixels, int width, int height, int stride, BarcodeReaderOptions options)
    {
        return run_on_native_stack(() => scan_gray8_count_only_core(pixels, width, height, stride, options));
    }

    public static int scan_gray8_collect_text(byte[] pixels, int width, int height, int stride, BarcodeReaderOptions options)
    {
        return run_on_native_stack(() => scan_gray8_collect_text_core(pixels, width, height, stride, options));
    }

    private static int scan_gray8_count_only_core(byte[] pixels, int width, int height, int stride, BarcodeReaderOptions options)
    {
        var nativeOptions = create_options(options);
        var state = new DirectScanState();
        var stateHandle = GCHandle.Alloc(state);
        var pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var status = NativeMethods.qsbc_loader_scan_gray8_cb_with_options(
                pixelsHandle.AddrOfPinnedObject(),
                checked((uint)width),
                checked((uint)height),
                checked((uint)stride),
                ref nativeOptions,
                CountOnlyCallback,
                GCHandle.ToIntPtr(stateHandle));

            if (status < 0)
            {
                throw new InvalidOperationException("Native scan failed with status " + status + ".");
            }

            return state.Count;
        }
        finally
        {
            pixelsHandle.Free();
            stateHandle.Free();
        }
    }

    private static int scan_gray8_collect_text_core(byte[] pixels, int width, int height, int stride, BarcodeReaderOptions options)
    {
        var nativeOptions = create_options(options);
        var state = new DirectScanState();
        var stateHandle = GCHandle.Alloc(state);
        var pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var status = NativeMethods.qsbc_loader_scan_gray8_cb_with_options(
                pixelsHandle.AddrOfPinnedObject(),
                checked((uint)width),
                checked((uint)height),
                checked((uint)stride),
                ref nativeOptions,
                CollectTextCallback,
                GCHandle.ToIntPtr(stateHandle));

            if (status < 0)
            {
                throw new InvalidOperationException("Native scan failed with status " + status + ".");
            }

            return state.Count + state.TextLength;
        }
        finally
        {
            pixelsHandle.Free();
            stateHandle.Free();
        }
    }

    private static T run_on_native_stack<T>(Func<T> action)
    {
        const int nativeBenchmarkStackSize = 16 * 1024 * 1024;
        T? result = default;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }, nativeBenchmarkStackSize)
        {
            IsBackground = true,
            Name = "QS Barcode direct benchmark scan"
        };
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }

        return result!;
    }

    private static NativeScanOptions create_options(BarcodeReaderOptions options)
    {
        var nativeOptions = new NativeScanOptions();
        var status = NativeMethods.qsbc_loader_scan_options_init(ref nativeOptions);
        if (status < 0)
        {
            throw new InvalidOperationException("Native options init failed with status " + status + ".");
        }

        nativeOptions.StructSize = (uint)Marshal.SizeOf<NativeScanOptions>();
        nativeOptions.Mask = (int)options.Symbologies;
        nativeOptions.MinLength = options.MinLength == 0 ? 1 : options.MinLength;
        nativeOptions.Flags = (uint)options.Flags;
        nativeOptions.PageStart = options.PageStart;
        nativeOptions.PageCount = options.PageCount;
        nativeOptions.Dpi = options.Dpi;
        nativeOptions.Reserved0 = options.DataMatrixFinderAngleTolerance;
        nativeOptions.Reserved1 = options.DataMatrixOverlapPercent;
        nativeOptions.Reserved2 = options.DataMatrixMaxLineCandidates;
        nativeOptions.Threshold = options.Threshold;
        nativeOptions.Orientation = (uint)options.Orientation;
        nativeOptions.MaxSkewDegrees = options.MaxSkewDegrees;
        nativeOptions.LightMargin = options.LightMargin;
        nativeOptions.ScanDistanceBarcode = options.ScanDistanceBarcode;
        nativeOptions.Tolerance = options.Tolerance;
        nativeOptions.MinHeight = options.MinHeight;
        nativeOptions.Percent = options.Percent;
        nativeOptions.ScanDistance = options.ScanDistance;
        nativeOptions.MaxGap = options.MaxGap;
        nativeOptions.MaxHeight = options.MaxHeight;
        nativeOptions.ChecksumFlags = options.ChecksumFlags;
        nativeOptions.ScanTimeoutMs = options.ScanTimeoutMs;
        return nativeOptions;
    }

    private static int count_only_callback(IntPtr result, IntPtr userData)
    {
        var state = (DirectScanState)GCHandle.FromIntPtr(userData).Target!;
        state.Count++;
        return 0;
    }

    private static int collect_text_callback(IntPtr result, IntPtr userData)
    {
        var state = (DirectScanState)GCHandle.FromIntPtr(userData).Target!;
        var native = Marshal.PtrToStructure<NativeBarcodeResult>(result);
        state.Count++;
        if (native.TextLen > 0)
        {
            state.TextLength += Marshal.PtrToStringUTF8(native.Text, native.TextLen)?.Length ?? 0;
        }
        return 0;
    }

    private sealed class DirectScanState
    {
        public int Count;
        public int TextLength;
    }
}
