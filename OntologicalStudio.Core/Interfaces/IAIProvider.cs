using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IAIProvider
{
    string ProviderName { get; }
    IAsyncEnumerable<AIChunk> StreamAsync(AIRequest request, CancellationToken cancellationToken = default);
    Task<HydrationResult> HydrateEntityAsync(Entity entity, HydrationOptions options);
    Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Entity entity);
    Task<string> GeneratePromptAsync(PromptContext context);
    Task<bool> ValidateConfigurationAsync();
}
