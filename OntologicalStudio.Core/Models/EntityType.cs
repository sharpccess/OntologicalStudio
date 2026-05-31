using System.ComponentModel.DataAnnotations.Schema;

namespace OntologicalStudio.Core.Models;

public class EntityType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SuggestedHydrationFields { get; set; } = string.Empty;
    public bool IsDefaultTemplate { get; set; }

    [NotMapped]
    public string? LocalizedName { get; set; }

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(LocalizedName) ? Name : LocalizedName;

    public ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
