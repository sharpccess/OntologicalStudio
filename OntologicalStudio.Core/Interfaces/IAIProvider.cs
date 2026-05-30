using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IAIProvider
{
    string ProviderName { get; }
    Task<HydrationResult> HydrateEntityAsync(Entity entity, HydrationOptions options);
    Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Entity entity);
    Task<string> GeneratePromptAsync(PromptContext context);
    Task<bool> ValidateConfigurationAsync();
}
