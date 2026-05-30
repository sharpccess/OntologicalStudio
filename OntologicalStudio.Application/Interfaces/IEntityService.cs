using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IEntityService
{
    Task<Entity> GetByIdAsync(Guid id);
    Task<IEnumerable<Entity>> GetByUniverseAsync(Guid universeId);
    Task<Entity> CreateAsync(string name, string description, Guid entityTypeId, Guid universeId);
    Task UpdateAsync(Entity entity);
    Task DeleteAsync(Guid id);
    Task AddTagToEntityAsync(Guid entityId, Guid tagId);
    Task RemoveTagFromEntityAsync(Guid entityId, Guid tagId);
}
