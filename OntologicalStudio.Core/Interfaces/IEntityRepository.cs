using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IEntityRepository
{
    Task<Entity> GetByIdAsync(Guid id);
    Task<IEnumerable<Entity>> GetAllAsync();
    Task<IEnumerable<Entity>> GetByUniverseAsync(Guid universeId);
    Task AddAsync(Entity entity);
    Task UpdateAsync(Entity entity);
    Task DeleteAsync(Entity entity);
}
