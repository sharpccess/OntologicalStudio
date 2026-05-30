namespace OntologicalStudio.Core.Models;

public class RelationshipType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Bidirectional { get; set; }
    public string AllowedSourceTypes { get; set; } = "[]";
    public string AllowedTargetTypes { get; set; } = "[]";

    public ICollection<Relationship> Relationships { get; set; } = new List<Relationship>();
}
