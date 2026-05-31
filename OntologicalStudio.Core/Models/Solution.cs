namespace OntologicalStudio.Core.Models;

public class Solution : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public Guid ScenarioId { get; set; }
    public Scenario? Scenario { get; set; }

    /// <summary>Exact prompt that was sent to the AI provider.</summary>
    public string PromptSnapshot { get; set; } = string.Empty;

    /// <summary>Provider name (OpenAI / Anthropic / Ollama / Heuristic / IdeBridge / etc.).</summary>
    public string ProviderUsed { get; set; } = string.Empty;

    /// <summary>Model id when applicable (gpt-4o, claude-3-5-sonnet, llama3, ...).</summary>
    public string ModelUsed { get; set; } = string.Empty;

    public SolutionStatus Status { get; set; } = SolutionStatus.Draft;

    /// <summary>0..5 user rating. 0 = unrated.</summary>
    public int Rating { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<SolutionArtifact> Artifacts { get; set; } = new List<SolutionArtifact>();
}
