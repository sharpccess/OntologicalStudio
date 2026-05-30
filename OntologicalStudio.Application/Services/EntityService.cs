using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class EntityService : IEntityService
{
    private readonly IEntityRepository _entityRepository;
    private readonly IEntityTypeRepository _entityTypeRepository;
    private readonly IUniverseRepository _universeRepository;
    private readonly ITagRepository _tagRepository;

    public EntityService(
        IEntityRepository entityRepository,
        IEntityTypeRepository entityTypeRepository,
        IUniverseRepository universeRepository,
        ITagRepository tagRepository)
    {
        _entityRepository = entityRepository;
        _entityTypeRepository = entityTypeRepository;
        _universeRepository = universeRepository;
        _tagRepository = tagRepository;
    }

    public async Task<Entity> GetByIdAsync(Guid id)
    {
        return await _entityRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Entity>> GetByUniverseAsync(Guid universeId)
    {
        return await _entityRepository.GetByUniverseAsync(universeId);
    }

    public async Task<Entity> CreateAsync(string name, string description, Guid entityTypeId, Guid universeId)
    {
        var entityType = await _entityTypeRepository.GetByIdAsync(entityTypeId);
        var universe = await _universeRepository.GetByIdAsync(universeId);

        if (entityType == null || universe == null)
            throw new InvalidOperationException("Entity type or universe not found");

        var entity = new Entity
        {
            Name = name,
            Description = description,
            EntityTypeId = entityTypeId,
            UniverseId = universeId
        };

        await _entityRepository.AddAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(Entity entity)
    {
        await _entityRepository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _entityRepository.GetByIdAsync(id);
        if (entity != null)
        {
            await _entityRepository.DeleteAsync(entity);
        }
    }

    public async Task AddTagToEntityAsync(Guid entityId, Guid tagId)
    {
        var entity = await _entityRepository.GetByIdAsync(entityId);
        var tag = await _tagRepository.GetByIdAsync(tagId);

        if (entity != null && tag != null && !entity.Tags.Contains(tag))
        {
            entity.Tags.Add(tag);
            await _entityRepository.UpdateAsync(entity);
        }
    }

    public async Task RemoveTagFromEntityAsync(Guid entityId, Guid tagId)
    {
        var entity = await _entityRepository.GetByIdAsync(entityId);
        var tag = await _tagRepository.GetByIdAsync(tagId);

        if (entity != null && tag != null && entity.Tags.Contains(tag))
        {
            entity.Tags.Remove(tag);
            await _entityRepository.UpdateAsync(entity);
        }
    }
}
