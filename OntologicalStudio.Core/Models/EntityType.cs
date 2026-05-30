namespace OntologicalStudio.Core.Models;

public class EntityType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SuggestedHydrationFields { get; set; } = string.Empty;
    public bool IsDefaultTemplate { get; set; }

    public ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
