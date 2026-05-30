using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IRelationshipTypeRepository
{
    Task<RelationshipType> GetByIdAsync(Guid id);
    Task<IEnumerable<RelationshipType>> GetAllAsync();
    Task AddAsync(RelationshipType relationshipType);
    Task UpdateAsync(RelationshipType relationshipType);
    Task DeleteAsync(RelationshipType relationshipType);
}
