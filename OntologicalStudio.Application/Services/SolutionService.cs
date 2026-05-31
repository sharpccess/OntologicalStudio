using OntologicalStudio.Application.Prompting;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class SolutionService : ISolutionService
{
    private readonly ISolutionRepository _repo;
    private readonly IScenarioService _scenarios;
    private readonly IUniverseService _universes;
    private readonly IEntityService _entities;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IAIProvider _ai;

    public SolutionService(
        ISolutionRepository repo,
        IScenarioService scenarios,
        IUniverseService universes,
        IEntityService entities,
        IPromptBuilder promptBuilder,
        IAIProvider ai)
    {
        _repo = repo;
        _scenarios = scenarios;
        _universes = universes;
        _entities = entities;
        _promptBuilder = promptBuilder;
        _ai = ai;
    }

    public Task<Solution?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

    public Task<IEnumerable<Solution>> GetByScenarioAsync(Guid scenarioId) =>
        _repo.GetByScenarioAsync(scenarioId);

    public async Task<Solution> RunAsync(Guid scenarioId, string? extraInstructions, string languageCode, CancellationToken ct = default)
    {
        var scenario = await _scenarios.GetByIdAsync(scenarioId)
            ?? throw new InvalidOperationException($"Scenario {scenarioId} not found.");

        var prompt = await _promptBuilder.BuildScenarioPromptAsync(scenarioId, extraInstructions, languageCode);

        var universe = await _universes.GetByIdAsync(scenario.UniverseId);
        var entityList = (await _entities.GetByUniverseAsync(scenario.UniverseId)).ToList();

        var ctx = new PromptContext
        {
            Universe = universe ?? new Universe(),
            Scenario = scenario,
            Entities = entityList,
            CurrentContext = prompt,
            OutputFormat = "markdown"
        };

        string responseText;
        string providerName;
        try
        {
            var builder = new System.Text.StringBuilder();
            await foreach (var chunk in _ai.StreamAsync(new AIRequest
            {
                UserPrompt = prompt,
                SystemPrompt = languageCode == "es"
                    ? "Responde exclusivamente en español. Usa markdown claro, estructurado y profesional."
                    : "Respond exclusively in English. Use clear, structured, professional markdown.",
                OutputFormat = "markdown"
            }, ct))
            {
                if (chunk is TextChunk textChunk)
                    builder.Append(textChunk.Text);
            }
            responseText = builder.ToString();
            providerName = _ai.ProviderName ?? "Unknown";
        }
        catch (Exception ex)
        {
            responseText = $"_AI provider failed:_ {ex.Message}\n\n--- Raw prompt ---\n\n{prompt}";
            providerName = "Error";
        }

        var solution = new Solution
        {
            Title = $"Solution {DateTime.Now:yyyy-MM-dd HH:mm}",
            ScenarioId = scenarioId,
            PromptSnapshot = prompt,
            ProviderUsed = providerName,
            ModelUsed = string.Empty,
            Status = SolutionStatus.Draft,
            Rating = 0,
            Notes = string.Empty
        };

        solution.Artifacts.Add(new SolutionArtifact
        {
            Kind = ArtifactKind.Markdown,
            MimeType = "text/markdown",
            InlineContent = responseText ?? string.Empty,
            SizeBytes = (responseText ?? string.Empty).Length,
            Order = 0,
            Label = languageCode == "es" ? "Respuesta de IA" : "AI response"
        });

        await _repo.AddAsync(solution);
        return solution;
    }

    public async Task DeleteAsync(Guid id)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s is null) return;
        await _repo.DeleteAsync(s);
    }

    public async Task UpdateRatingAsync(Guid id, int rating)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s is null) return;
        s.Rating = Math.Clamp(rating, 0, 5);
        await _repo.UpdateAsync(s);
    }

    public async Task UpdateNotesAsync(Guid id, string notes)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s is null) return;
        s.Notes = notes ?? string.Empty;
        await _repo.UpdateAsync(s);
    }

    public async Task UpdateStatusAsync(Guid id, SolutionStatus status)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s is null) return;
        s.Status = status;
        await _repo.UpdateAsync(s);
    }
}
