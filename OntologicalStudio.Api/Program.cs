using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Infrastructure;
using OntologicalStudio.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "OntologicalStudio");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "ontology.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DatabaseSeeder.SeedAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/universes", async ([FromServices] IUniverseService universes) =>
{
    var items = await universes.GetAllAsync();
    return Results.Ok(items.Select(x => new
    {
        x.Id,
        x.Name,
        x.Description,
        x.CreatedAt
    }));
});

app.MapGet("/api/universes/{id:guid}", async (Guid id, [FromServices] IUniverseService universes) =>
{
    var universe = await universes.GetByIdAsync(id);
    return universe is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            universe.Id,
            universe.Name,
            universe.Description,
            universe.Metadata,
            universe.CreatedAt
        });
});

app.MapGet("/api/universes/{id:guid}/entities", async (Guid id, [FromServices] IEntityService entities) =>
{
    var items = await entities.GetByUniverseAsync(id);
    return Results.Ok(items.Select(x => new
    {
        x.Id,
        x.Name,
        x.Description,
        x.EntityTypeId,
        EntityType = x.EntityType?.Name,
        x.PositionX,
        x.PositionY,
        x.Properties,
        x.Notes,
        x.HydrationData,
        x.ConfidenceLevel,
        x.CompletenessScore
    }));
});

app.MapGet("/api/universes/{id:guid}/scenarios", async (Guid id, [FromServices] IScenarioService scenarios) =>
{
    var items = await scenarios.GetByUniverseAsync(id);
    return Results.Ok(items.Select(x => new
    {
        x.Id,
        x.Title,
        x.Description,
        x.Status,
        x.Goals,
        x.CreatedAt
    }));
});

app.MapPost("/api/scenarios/{id:guid}/solve", async (
    Guid id,
    [FromBody] SolveScenarioRequest request,
    [FromServices] ISolutionService solutions,
    CancellationToken cancellationToken) =>
{
    var solution = await solutions.RunAsync(id, request.ExtraInstructions, cancellationToken);
    return Results.Ok(new
    {
        solution.Id,
        solution.Title,
        solution.ProviderUsed,
        solution.Status,
        Artifacts = solution.Artifacts.Select(a => new
        {
            a.Id,
            a.Kind,
            a.Label,
            a.MimeType,
            a.InlineContent,
            a.BlobPath
        })
    });
});

app.Run("http://127.0.0.1:53821");

public record SolveScenarioRequest(string? ExtraInstructions);