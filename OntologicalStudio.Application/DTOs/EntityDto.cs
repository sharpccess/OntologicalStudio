namespace OntologicalStudio.Application.DTOs;

public class EntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid EntityTypeId { get; set; }
    public string EntityTypeName { get; set; } = string.Empty;
    public Guid UniverseId { get; set; }
    public string UniverseName { get; set; } = string.Empty;
    public string Properties { get; set; } = "{}";
    public string Notes { get; set; } = string.Empty;
    public string HydrationData { get; set; } = "{}";
    public int ConfidenceLevel { get; set; }
    public int CompletenessScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
