using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IEntityHydrationWorkflowService
{
    Task<HydrationPreview> PreviewHydrationAsync(Guid entityId, HydrationOptions options, string? customPrompt = null, string languageCode = "en");
    Task<HydrationLog> ApplyHydrationAsync(Guid entityId, HydrationApplyRequest request);
    Task<IEnumerable<HydrationLog>> GetHistoryAsync(Guid entityId);
}

public class HydrationPreview
{
    public Guid EntityId { get; set; }
    public string PromptUsed { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public string CurrentHydrationData { get; set; } = "{}";
    public string CurrentNotes { get; set; } = string.Empty;
    public int CurrentConfidenceLevel { get; set; }
    public int CurrentCompletenessScore { get; set; }
    public HydrationResult Result { get; set; } = new();
}

public class HydrationApplyRequest
{
    public Guid EntityId { get; set; }
    public string PromptUsed { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public HydrationResult Preview { get; set; } = new();
    public bool ApplyHydrationData { get; set; } = true;
    public bool ApplyNotes { get; set; } = true;
    public bool ApplyConfidence { get; set; } = true;
    public bool ApplyCompleteness { get; set; } = true;
}