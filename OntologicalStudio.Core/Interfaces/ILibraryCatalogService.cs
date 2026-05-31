using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface ILibraryCatalogService
{
    Task<IReadOnlyList<EntityLibraryItem>> GetEntityItemsAsync();
    Task<IReadOnlyList<UniverseModelLibraryItem>> GetUniverseModelItemsAsync();
    Task<EntityLibraryItem> SaveEntityAsync(EntityLibraryItem item);
    Task<UniverseModelLibraryItem> SaveUniverseModelAsync(UniverseModelLibraryItem item);
    Task DeleteEntityAsync(Guid id);
    Task DeleteUniverseModelAsync(Guid id);
}