using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IAIProvider
{
    string ProviderName { get; }
    IAsyncEnumerable<AIChunk> StreamAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<HydrationResult> HydrateEntityAsync(Entity entity, HydrationOptions options, string? customPrompt = null, string languageCode = "en", WebResearchResult? webResearch = null);
    Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Entity entity);
    Task<string> GeneratePromptAsync(PromptContext context);
    Task<bool> ValidateConfigurationAsync();
}
