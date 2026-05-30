namespace OntologicalStudio.Core.Models;

public class Tag : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
