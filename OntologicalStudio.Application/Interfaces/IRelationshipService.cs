using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IRelationshipService
{
    Task<Relationship> GetByIdAsync(Guid id);
    Task<IEnumerable<Relationship>> GetBySourceEntityAsync(Guid entityId);
    Task<IEnumerable<Relationship>> GetByTargetEntityAsync(Guid entityId);
    Task<Relationship> CreateAsync(Guid sourceEntityId, Guid targetEntityId, Guid relationshipTypeId);
    Task UpdateAsync(Relationship relationship);
    Task DeleteAsync(Guid id);
}
