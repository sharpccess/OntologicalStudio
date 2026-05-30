namespace OntologicalStudio.Core.Models;

public class RelationshipSuggestion
{
    public Guid TargetEntityId { get; set; }
    public string RelationshipTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
