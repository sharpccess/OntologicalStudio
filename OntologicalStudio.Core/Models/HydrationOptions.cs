namespace OntologicalStudio.Core.Models;

public class HydrationOptions
{
    public bool IncludePersonalities { get; set; }
    public bool IncludeMotivations { get; set; }
    public bool IncludeFears { get; set; }
    public bool IncludeIncentives { get; set; }
    public bool IncludeBehavioralPatterns { get; set; }
    public int MaxSuggestions { get; set; } = 5;
    public string HydrationMode { get; set; } = "factual";
    
    public bool AutoApprove { get; set; }
    public float Temperature { get; set; }
    public int DetailLevel { get; set; }
}
