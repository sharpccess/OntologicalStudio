namespace OntologicalStudio.Core.Models;

public class EntityLibraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityTypeName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string HydrationData { get; set; } = string.Empty;
    public int ConfidenceLevel { get; set; }
    public int CompletenessScore { get; set; }
    public string Properties { get; set; } = "{}";
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UniverseModelLibraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<LibraryEntitySnapshot> Entities { get; set; } = new();
    public List<LibraryRelationshipSnapshot> Relationships { get; set; } = new();
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}

public class LibraryEntitySnapshot
{
    public Guid SnapshotId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityTypeName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string HydrationData { get; set; } = string.Empty;
    public int ConfidenceLevel { get; set; }
    public int CompletenessScore { get; set; }
    public string Properties { get; set; } = "{}";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

public class LibraryRelationshipSnapshot
{
    public Guid SourceSnapshotId { get; set; }
    public Guid TargetSnapshotId { get; set; }
    public string RelationshipTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Properties { get; set; } = "{}";
}

public class LibraryCatalog
{
    public List<EntityLibraryItem> Entities { get; set; } = new();
    public List<UniverseModelLibraryItem> UniverseModels { get; set; } = new();
}