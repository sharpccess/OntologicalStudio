using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Application.Prompting;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Infrastructure.Storage;
using OntologicalStudio.Infrastructure.Services;
using OntologicalStudio.Persistence.Context;
using OntologicalStudio.Persistence.Repositories;
using OntologicalStudio.Application.Services;
using OntologicalStudio.AIProviders;

namespace OntologicalStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string? blobRootDirectory = null)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<IAiConnectionSettingsService, AiConnectionSettingsService>();
        services.AddSingleton<ILibraryCatalogService, LibraryCatalogService>();
        services.AddScoped<IAIProvider, ConfigurableAIProvider>();

        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IUniverseRepository, UniverseRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IEntityTypeRepository, EntityTypeRepository>();
        services.AddScoped<IRelationshipTypeRepository, RelationshipTypeRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISolutionRepository, SolutionRepository>();
        services.AddScoped<IHydrationLogRepository, HydrationLogRepository>();

        services.AddScoped<IUniverseService, UniverseService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<IAIHydrationService, AIHydrationService>();
        services.AddScoped<IEntityHydrationWorkflowService, EntityHydrationWorkflowService>();
        services.AddSingleton<IWebResearchService, WebResearchService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IArtifactExportService, ArtifactExportService>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<ISolutionService, SolutionService>();

        var blobRoot = blobRootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OntologicalStudio", "blobs");
        services.AddSingleton<IBlobStore>(_ => new FileSystemBlobStore(blobRoot));

        return services;
    }
}
