namespace OntologicalStudio.Core.Models;

public class PromptContext
{
    public Universe Universe { get; set; } = new Universe();
    public Scenario Scenario { get; set; } = new Scenario();
    public List<Entity> Entities { get; set; } = new List<Entity>();
    public string CurrentContext { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "text";
}
