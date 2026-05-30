using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IEntityTypeRepository
{
    Task<EntityType> GetByIdAsync(Guid id);
    Task<IEnumerable<EntityType>> GetAllAsync();
    Task AddAsync(EntityType entityType);
    Task UpdateAsync(EntityType entityType);
    Task DeleteAsync(EntityType entityType);
}
