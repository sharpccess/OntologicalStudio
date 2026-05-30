namespace OntologicalStudio.Core.Models;

public class Relationship : EntityBase
{
    public Guid SourceEntityId { get; set; }
    public Entity SourceEntity { get; set; } = new Entity();
    public Guid TargetEntityId { get; set; }
    public Entity TargetEntity { get; set; } = new Entity();
    public Guid RelationshipTypeId { get; set; }
    public RelationshipType RelationshipType { get; set; } = new RelationshipType();
    public string Properties { get; set; } = "{}";
    public string Description { get; set; } = string.Empty;
}
