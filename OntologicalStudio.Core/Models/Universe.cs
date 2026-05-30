namespace OntologicalStudio.Core.Models;

public class Universe : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string Metadata { get; set; } = "{}";

    public ICollection<Entity> Entities { get; set; } = new List<Entity>();
    public ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
}
