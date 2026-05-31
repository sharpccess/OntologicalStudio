using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class RelationshipService : IRelationshipService
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly IRelationshipTypeRepository _relationshipTypeRepository;

    public RelationshipService(
        IRelationshipRepository relationshipRepository,
        IEntityRepository entityRepository,
        IRelationshipTypeRepository relationshipTypeRepository)
    {
        _relationshipRepository = relationshipRepository;
        _entityRepository = entityRepository;
        _relationshipTypeRepository = relationshipTypeRepository;
    }

    public async Task<Relationship> GetByIdAsync(Guid id)
    {
        return await _relationshipRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Relationship>> GetBySourceEntityAsync(Guid entityId)
    {
        return await _relationshipRepository.GetBySourceEntityAsync(entityId);
    }

    public async Task<IEnumerable<Relationship>> GetByTargetEntityAsync(Guid entityId)
    {
        return await _relationshipRepository.GetByTargetEntityAsync(entityId);
    }

    public async Task<Relationship> CreateAsync(Guid sourceEntityId, Guid targetEntityId, Guid relationshipTypeId)
    {
        var sourceEntity = await _entityRepository.GetByIdAsync(sourceEntityId);
        var targetEntity = await _entityRepository.GetByIdAsync(targetEntityId);
        var relationshipType = await _relationshipTypeRepository.GetByIdAsync(relationshipTypeId);

        if (sourceEntity == null || targetEntity == null || relationshipType == null)
            throw new InvalidOperationException("Entity or relationship type not found");

        var existingRelationship = (await _relationshipRepository.GetBySourceEntityAsync(sourceEntityId))
            .FirstOrDefault(r => r.TargetEntityId == targetEntityId && !r.IsDeleted);

        if (existingRelationship is not null)
        {
            existingRelationship.RelationshipTypeId = relationshipTypeId;
            existingRelationship.RelationshipType = null!;
            await _relationshipRepository.UpdateAsync(existingRelationship);
            return existingRelationship;
        }

        var relationship = new Relationship
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipTypeId = relationshipTypeId
        };

        await _relationshipRepository.AddAsync(relationship);
        return relationship;
    }

    public async Task UpdateAsync(Relationship relationship)
    {
        await _relationshipRepository.UpdateAsync(relationship);
    }

    public async Task DeleteAsync(Guid id)
    {
        var relationship = await _relationshipRepository.GetByIdAsync(id);
        if (relationship != null)
        {
            await _relationshipRepository.DeleteAsync(relationship);
        }
    }
}
