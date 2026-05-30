namespace OntologicalStudio.Core.Models;

public class EntityScenario
{
    public Guid EntityId { get; set; }
    public Guid ScenarioId { get; set; }
    public string? Role { get; set; }
}

public class EntityTag
{
    public Guid EntityId { get; set; }
    public Guid TagId { get; set; }
}
