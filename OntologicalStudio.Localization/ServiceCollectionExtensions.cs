using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OntologicalStudio.Localization.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalization(this IServiceCollection services, string languagesDirectory)
    {
        services.AddSingleton<ILocalizationService>(sp =>
        {
            var localization = new LocalizationService(languagesDirectory);
            localization.Initialize(languagesDirectory);
            return localization;
        });
        return services;
    }
}
