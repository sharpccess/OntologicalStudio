using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IRelationshipRepository
{
    Task<Relationship> GetByIdAsync(Guid id);
    Task<IEnumerable<Relationship>> GetBySourceEntityAsync(Guid entityId);
    Task<IEnumerable<Relationship>> GetByTargetEntityAsync(Guid entityId);
    Task AddAsync(Relationship relationship);
    Task UpdateAsync(Relationship relationship);
    Task DeleteAsync(Relationship relationship);
}
