namespace QualitySoft.Barcode;

/// <summary>
/// Reader settings for managed defaults.
/// </summary>
public sealed class BarcodeReaderSettings
{
    /// <summary>
    /// Managed scan defaults used whenever a read call does not pass options explicitly.
    /// </summary>
    public BarcodeReaderOptions DefaultOptions { get; set; } = new BarcodeReaderOptions();

    internal BarcodeReaderSettings Clone()
    {
        return new BarcodeReaderSettings
        {
            DefaultOptions = DefaultOptions.Clone()
        };
    }

    internal void Validate()
    {
        if (DefaultOptions == null)
        {
            throw new ArgumentNullException(nameof(DefaultOptions));
        }

        DefaultOptions.Validate();
    }
}
