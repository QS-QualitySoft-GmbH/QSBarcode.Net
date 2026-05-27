using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QualitySoft.Barcode;

/// <summary>
/// Dependency injection registration helpers for the QS Barcode SDK.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBarcodeReader"/> as a singleton service.
    /// </summary>
    public static IServiceCollection AddQualitySoftBarcode(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IBarcodeReader, QualitySoftBarcodeReader>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IBarcodeReader"/> as a singleton service with managed default scan options.
    /// Explicit options passed to read calls still override these defaults.
    /// </summary>
    public static IServiceCollection AddQualitySoftBarcode(this IServiceCollection services, Action<BarcodeReaderOptions> configureDefaults)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureDefaults == null)
        {
            throw new ArgumentNullException(nameof(configureDefaults));
        }

        var defaults = new BarcodeReaderOptions();
        configureDefaults(defaults);
        defaults.Validate();

        services.TryAddSingleton<IBarcodeReader>(_ => new QualitySoftBarcodeReader(defaults));
        return services;
    }

    /// <summary>
    /// Registers <see cref="IBarcodeReader"/> as a singleton service with explicit reader settings.
    /// </summary>
    public static IServiceCollection AddQualitySoftBarcode(this IServiceCollection services, BarcodeReaderSettings settings)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        settings.Validate();
        var snapshot = settings.Clone();
        services.TryAddSingleton<IBarcodeReader>(_ => new QualitySoftBarcodeReader(snapshot));
        return services;
    }
}
