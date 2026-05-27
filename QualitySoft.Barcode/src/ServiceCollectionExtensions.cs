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
        services.TryAddSingleton<IBarcodeReader, QualitySoftBarcodeReader>();
        return services;
    }
}
