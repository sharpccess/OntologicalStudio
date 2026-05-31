using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OntologicalStudio.Application.Services;

public class AIHydrationService : IAIHydrationService
{
    private readonly IAIProvider _aiProvider;
    private readonly IEntityRepository _entityRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IWebResearchService _webResearchService;

    public AIHydrationService(
        IAIProvider aiProvider,
        IEntityRepository entityRepository,
        IScenarioRepository scenarioRepository,
        IRelationshipRepository relationshipRepository,
        IWebResearchService webResearchService)
    {
        _aiProvider = aiProvider;
        _entityRepository = entityRepository;
        _scenarioRepository = scenarioRepository;
        _relationshipRepository = relationshipRepository;
        _webResearchService = webResearchService;
    }

    public async Task<HydrationResult> HydrateEntityAsync(Guid entityId, HydrationOptions options, string? customPrompt = null, string languageCode = "en")
    {
        var entity = await _entityRepository.GetByIdAsync(entityId);
        if (entity == null)
            throw new InvalidOperationException("Entity not found");

        var webResearch = await _webResearchService.ResearchAsync(
            BuildResearchQuery(entity, customPrompt, languageCode),
            languageCode);

        return await _aiProvider.HydrateEntityAsync(entity, options, customPrompt, languageCode, webResearch);
    }

    public async Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Guid entityId)
    {
        var entity = await _entityRepository.GetByIdAsync(entityId);
        if (entity == null)
            throw new InvalidOperationException("Entity not found");

        return await _aiProvider.SuggestRelationshipsAsync(entity);
    }

    public async Task<string> GeneratePromptAsync(PromptContext context)
    {
        return await _aiProvider.GeneratePromptAsync(context);
    }

    public async Task<string> GeneratePromptStreamingAsync(PromptContext context, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        await foreach (var chunk in _aiProvider.StreamAsync(new AIRequest
        {
            UserPrompt = context.CurrentContext,
            SystemPrompt = context.OutputFormat,
            OutputFormat = context.OutputFormat
        }, cancellationToken))
        {
            if (chunk is TextChunk textChunk)
                builder.Append(textChunk.Text);
        }

        return builder.ToString();
    }

    public async Task<string> AnalyzeScenarioAsync(Guid scenarioId)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);
        if (scenario == null)
            throw new InvalidOperationException("Scenario not found");

        // Fetch all entities in the same universe to provide complete context
        var entities = await _entityRepository.GetByUniverseAsync(scenario.UniverseId);
        var entityList = entities.ToList();

        // Fetch all relationships in the universe
        var relationshipsList = new List<Relationship>();
        foreach (var entity in entityList)
        {
            var rels = await _relationshipRepository.GetBySourceEntityAsync(entity.Id);
            foreach (var r in rels)
            {
                if (!relationshipsList.Any(existing => existing.Id == r.Id))
                {
                    relationshipsList.Add(r);
                }
            }
        }

        // Construct a structured prompt detailing the problem context
        var sb = new StringBuilder();
        sb.AppendLine("### CONTEXT MODEL FOR REASONING");
        sb.AppendLine($"**Problem Title:** {scenario.Title}");
        sb.AppendLine($"**Situation Description:** {scenario.Description}");
        sb.AppendLine($"**Objectives & Constraints:** {scenario.Goals}");
        sb.AppendLine();
        
        sb.AppendLine("### KEY ENTITIES");
        foreach (var entity in entityList)
        {
            sb.AppendLine($"- **{entity.Name}** ({entity.EntityType?.Name ?? "General Entity"})");
            if (!string.IsNullOrEmpty(entity.Description))
                sb.AppendLine($"  _Description:_ {entity.Description}");
            if (!string.IsNullOrEmpty(entity.Notes))
                sb.AppendLine($"  _Internal Dynamics/Notes:_ {entity.Notes}");
            sb.AppendLine($"  _Confidence:_ {entity.ConfidenceLevel}% | _Completeness:_ {entity.CompletenessScore}%");
        }
        sb.AppendLine();

        sb.AppendLine("### RELATIONSHIP DYNAMICS");
        foreach (var rel in relationshipsList)
        {
            var sourceName = entityList.FirstOrDefault(e => e.Id == rel.SourceEntityId)?.Name ?? "Unknown";
            var targetName = entityList.FirstOrDefault(e => e.Id == rel.TargetEntityId)?.Name ?? "Unknown";
            sb.AppendLine($"- **{sourceName}** ──({rel.RelationshipType?.Name ?? "influences"})──&gt; **{targetName}**");
            if (!string.IsNullOrEmpty(rel.Description))
                sb.AppendLine($"  _Influence Details:_ {rel.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("### INSTRUCTIONS FOR REASONING");
        sb.AppendLine("Act as a professional strategic analyst and cognitive consultant. Analyze the provided model and generate a detailed report addressing:");
        sb.AppendLine("1. **Executive Situation Summary**: Synthesize the core problem.");
        sb.AppendLine("2. **Stakeholder & Systemic Dynamics**: Detail how the entities impact each other.");
        sb.AppendLine("3. **Contradictions & Blind Spots**: Highlight hidden patterns (e.g., trust mismatches, operational bottlenecks, conflicting incentives, emotional fears).");
        sb.AppendLine("4. **Scenario Risk Assessment**: What happens if no intervention is made?");
        sb.AppendLine("5. **Strategic Intervention Recommendations**: Actionable proposals (organizational redesign, habits, incentives, or personal reflection steps).");
        sb.AppendLine("6. **Action Priorities**: Top 3 high-impact steps.");

        // Call the AI provider to run reasoning
        var contextObj = new PromptContext
        {
            CurrentContext = sb.ToString(),
            OutputFormat = "You are a professional system thinking advisor. Use structured, clear, and analytical language. Format your response in clean markdown."
        };

        return await _aiProvider.GeneratePromptAsync(contextObj);
    }

    private static string BuildResearchQuery(Entity entity, string? customPrompt, string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
            return $"{entity.Name} {entity.EntityType?.Name} {customPrompt}";

        return languageCode == "es"
            ? $"{entity.Name} {entity.EntityType?.Name} noticias actuales contexto"
            : $"{entity.Name} {entity.EntityType?.Name} current news context";
    }
}
