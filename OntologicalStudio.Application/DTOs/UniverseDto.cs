namespace OntologicalStudio.Application.DTOs;

public class UniverseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int EntityCount { get; set; }
    public int ScenarioCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
