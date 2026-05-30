namespace OntologicalStudio.Core.Models;

public class HydrationResult
{
    public Guid EntityId { get; set; }
    public string SuggestedProperties { get; set; } = "{}";
    public string SuggestedNotes { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public List<string> Sources { get; set; } = new List<string>();
    
    public int CompletenessScore { get; set; }
    public string SuggestedPropertiesJson { get; set; } = "{}";
    public string AnalysisNotes { get; set; } = string.Empty;
}
