namespace OntologicalStudio.Core.Models;

public class HydrationLog : EntityBase
{
    public Guid EntityId { get; set; }
    public Entity? Entity { get; set; }

    public string PromptUsed { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public string AppliedFields { get; set; } = "[]";
}