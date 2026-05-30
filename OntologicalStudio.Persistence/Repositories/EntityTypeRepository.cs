using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class EntityTypeRepository : IEntityTypeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<EntityType> _entityTypes;

    public EntityTypeRepository(ApplicationDbContext context)
    {
        _context = context;
        _entityTypes = context.Set<EntityType>();
    }

    public async Task<EntityType> GetByIdAsync(Guid id)
    {
        return await _entityTypes.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<IEnumerable<EntityType>> GetAllAsync()
    {
        return await _entityTypes
            .Where(e => !e.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(EntityType entityType)
    {
        entityType.Id = Guid.NewGuid();
        entityType.CreatedAt = DateTime.UtcNow;
        await _entityTypes.AddAsync(entityType);
    }

    public async Task UpdateAsync(EntityType entityType)
    {
        entityType.UpdatedAt = DateTime.UtcNow;
        _entityTypes.Update(entityType);
    }

    public async Task DeleteAsync(EntityType entityType)
    {
        entityType.IsDeleted = true;
        entityType.UpdatedAt = DateTime.UtcNow;
        _entityTypes.Update(entityType);
    }
}
