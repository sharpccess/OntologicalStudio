namespace OntologicalStudio.Core.Models;

public class Entity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid EntityTypeId { get; set; }
    public EntityType EntityType { get; set; } = new EntityType();
    public Guid UniverseId { get; set; }
    public Universe Universe { get; set; } = new Universe();
    public string Properties { get; set; } = "{}";
    public string Notes { get; set; } = string.Empty;
    public string HydrationData { get; set; } = "{}";
    public int ConfidenceLevel { get; set; }
    public int CompletenessScore { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }

    public ICollection<Relationship> SourceRelationships { get; set; } = new List<Relationship>();
    public ICollection<Relationship> TargetRelationships { get; set; } = new List<Relationship>();
    public ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
