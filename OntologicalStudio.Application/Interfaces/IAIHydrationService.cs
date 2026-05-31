using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IAIHydrationService
{
    Task<HydrationResult> HydrateEntityAsync(Guid entityId, HydrationOptions options, string? customPrompt = null, string languageCode = "en");
    Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Guid entityId);
    Task<string> GeneratePromptAsync(PromptContext context);
    Task<string> AnalyzeScenarioAsync(Guid scenarioId);
}
