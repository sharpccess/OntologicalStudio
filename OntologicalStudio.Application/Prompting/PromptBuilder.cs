using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System.Text;

namespace OntologicalStudio.Application.Prompting;

public class PromptBuilder : IPromptBuilder
{
    private readonly IScenarioService _scenarios;
    private readonly IUniverseService _universes;
    private readonly IEntityService _entities;
    private readonly IRelationshipRepository _relationships;

    public PromptBuilder(
        IScenarioService scenarios,
        IUniverseService universes,
        IEntityService entities,
        IRelationshipRepository relationships)
    {
        _scenarios = scenarios;
        _universes = universes;
        _entities = entities;
        _relationships = relationships;
    }

    public async Task<string> BuildScenarioPromptAsync(Guid scenarioId, string? extraInstructions = null)
    {
        var scenario = await _scenarios.GetByIdAsync(scenarioId)
            ?? throw new InvalidOperationException($"Scenario {scenarioId} not found.");
        var universe = await _universes.GetByIdAsync(scenario.UniverseId)
            ?? throw new InvalidOperationException($"Universe {scenario.UniverseId} not found.");
        var entities = (await _entities.GetByUniverseAsync(universe.Id)).ToList();

        var seen = new HashSet<Guid>();
        var rels = new List<Relationship>();
        foreach (var e in entities)
        {
            var rs = await _relationships.GetBySourceEntityAsync(e.Id);
            foreach (var r in rs)
                if (seen.Add(r.Id)) rels.Add(r);
        }

        return BuildScenarioPrompt(universe, scenario, entities, rels, extraInstructions);
    }

    public string BuildScenarioPrompt(
        Universe universe,
        Scenario scenario,
        IReadOnlyList<Entity> entities,
        IReadOnlyList<Relationship> relationships,
        string? extraInstructions = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### CONTEXT MODEL FOR REASONING");
        sb.AppendLine($"**Universe:** {universe.Name}");
        if (!string.IsNullOrWhiteSpace(universe.Description))
            sb.AppendLine($"**Universe Description:** {universe.Description}");
        sb.AppendLine();
        sb.AppendLine($"**Problem Title:** {scenario.Title}");
        sb.AppendLine($"**Situation Description:** {scenario.Description}");
        if (!string.IsNullOrWhiteSpace(scenario.Goals))
            sb.AppendLine($"**Objectives & Constraints:** {scenario.Goals}");
        sb.AppendLine();

        sb.AppendLine("### KEY ENTITIES");
        if (entities.Count == 0)
            sb.AppendLine("_(no entities defined yet)_");
        foreach (var e in entities)
        {
            sb.AppendLine($"- **{e.Name}** ({e.EntityType?.Name ?? "Entity"})");
            if (!string.IsNullOrWhiteSpace(e.Description))
                sb.AppendLine($"  Description: {e.Description}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
            sb.AppendLine($"  Confidence: {e.ConfidenceLevel}% | Completeness: {e.CompletenessScore}%");
        }
        sb.AppendLine();

        sb.AppendLine("### RELATIONSHIPS");
        if (relationships.Count == 0)
            sb.AppendLine("_(no relationships defined yet)_");
        foreach (var r in relationships)
        {
            var src = entities.FirstOrDefault(x => x.Id == r.SourceEntityId)?.Name ?? "?";
            var tgt = entities.FirstOrDefault(x => x.Id == r.TargetEntityId)?.Name ?? "?";
            var type = r.RelationshipType?.Name ?? "relatesTo";
            sb.AppendLine($"- {src} --({type})--> {tgt}");
            if (!string.IsNullOrWhiteSpace(r.Description))
                sb.AppendLine($"  {r.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("### REASONING INSTRUCTIONS");
        sb.AppendLine("Act as a strategic systems analyst. Using the model above:");
        sb.AppendLine("1. Summarize the core situation.");
        sb.AppendLine("2. Identify systemic dynamics and feedback loops between entities.");
        sb.AppendLine("3. Surface contradictions, blind spots, and hidden incentives.");
        sb.AppendLine("4. Assess risks if no intervention is made.");
        sb.AppendLine("5. Propose 3-5 high-leverage interventions, ranked by impact.");

        if (!string.IsNullOrWhiteSpace(extraInstructions))
        {
            sb.AppendLine();
            sb.AppendLine("### ADDITIONAL INSTRUCTIONS");
            sb.AppendLine(extraInstructions);
        }
        return sb.ToString();
    }
}
