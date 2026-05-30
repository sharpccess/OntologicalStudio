namespace OntologicalStudio.Core.Models;

public enum ScenarioStatus
{
    Draft,
    Active,
    Resolved
}

public class Scenario : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UniverseId { get; set; }
    public Universe Universe { get; set; } = new Universe();
    public string Context { get; set; } = "{}";
    public string Goals { get; set; } = string.Empty;
    public ScenarioStatus Status { get; set; }
    public string Results { get; set; } = "{}";

    public ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
