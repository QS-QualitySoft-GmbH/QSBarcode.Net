using System.Text;
using QualitySoft.Barcode;

if (args.Length < 1 || !string.Equals(args[0], "demo-license-probe", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Usage: demo-license-probe <symbology> <fixture> <base64-expected-text>...");
    return 64;
}

if (args.Length < 3)
{
    Console.Error.WriteLine("Missing symbology or fixture arguments.");
    return 64;
}

var symbology = ParseSymbology(args[1]);
var fixture = args[2];
var expectedTexts = args.Skip(3)
    .Select(value => Encoding.UTF8.GetString(Convert.FromBase64String(value)))
    .Where(value => value.Length > 0)
    .ToArray();

try
{
    var status = BarcodeLicense.GetStatus();
    Console.WriteLine("license=" + status.Features);
    if (!status.IsDemo)
    {
        Console.Error.WriteLine("Expected demo license status without license file.");
        return 5;
    }

    using var reader = new QualitySoftBarcodeReader(new BarcodeReaderSettings
    {
        DefaultOptions = new BarcodeReaderOptions
        {
            Symbologies = symbology,
            MinLength = 1,
            ScanTimeoutMs = 15_000
        }
    });

    var demoTexts = reader.Read(fixture).Select(result => result.Text).ToArray();
    Console.WriteLine("results=" + string.Join("|", demoTexts));

    if (demoTexts.Length == 0)
    {
        Console.Error.WriteLine("Demo scan returned no result.");
        return 3;
    }

    if (expectedTexts.Length > 0 && demoTexts.Any(text => expectedTexts.Contains(text, StringComparer.Ordinal)))
    {
        return 2;
    }

    return 0;
}
catch (BarcodeScanException ex) when (ex.StatusCode == -6 || string.Equals(ex.StatusName, "license_required", StringComparison.Ordinal))
{
    Console.WriteLine("license_required");
    return 4;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static BarcodeSymbology ParseSymbology(string value)
{
    return value.ToLowerInvariant() switch
    {
        "datamatrix" => BarcodeSymbology.DataMatrix,
        "qr" => BarcodeSymbology.Qr,
        "aztec" => BarcodeSymbology.Aztec,
        "pdf417" => BarcodeSymbology.Pdf417,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported demo probe symbology.")
    };
}
