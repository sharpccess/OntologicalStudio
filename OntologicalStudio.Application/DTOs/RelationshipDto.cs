namespace OntologicalStudio.Application.DTOs;

public class RelationshipDto
{
    public Guid Id { get; set; }
    public Guid SourceEntityId { get; set; }
    public string SourceEntityName { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string TargetEntityName { get; set; } = string.Empty;
    public Guid RelationshipTypeId { get; set; }
    public string RelationshipTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
