namespace OntologicalStudio.Application.DTOs;

public class ScenarioDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UniverseId { get; set; }
    public string UniverseName { get; set; } = string.Empty;
    public int EntityCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
