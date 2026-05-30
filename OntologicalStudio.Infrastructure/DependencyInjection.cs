using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Persistence.Context;
using OntologicalStudio.Persistence.Repositories;
using OntologicalStudio.Application.Services;
using OntologicalStudio.AIProviders;

namespace OntologicalStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IAIProvider, ConfigurableAIProvider>();

        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IUniverseRepository, UniverseRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IEntityTypeRepository, EntityTypeRepository>();
        services.AddScoped<IRelationshipTypeRepository, RelationshipTypeRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        services.AddScoped<IUniverseService, UniverseService>();
        services.AddScoped<IEntityService, EntityService>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<IAIHydrationService, AIHydrationService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
