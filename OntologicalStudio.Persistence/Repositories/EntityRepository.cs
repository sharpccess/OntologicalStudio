using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class EntityRepository : IEntityRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Entity> _entities;

    public EntityRepository(ApplicationDbContext context)
    {
        _context = context;
        _entities = context.Set<Entity>();
    }

    public async Task<Entity> GetByIdAsync(Guid id)
    {
        return await _entities
            .Include(e => e.EntityType)
            .Include(e => e.Universe)
            .Include(e => e.SourceRelationships).ThenInclude(r => r.RelationshipType)
            .Include(e => e.TargetRelationships).ThenInclude(r => r.RelationshipType)
            .Include(e => e.Tags)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<IEnumerable<Entity>> GetAllAsync()
    {
        return await _entities
            .Include(e => e.EntityType)
            .Include(e => e.Universe)
            .Where(e => !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Entity>> GetByUniverseAsync(Guid universeId)
    {
        return await _entities
            .Include(e => e.EntityType)
            .Include(e => e.Universe)
            .Where(e => e.UniverseId == universeId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(Entity entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        // Avoid EF tracking duplicate references coming from default-initialized navigation props
        entity.EntityType = null!;
        entity.Universe = null!;
        await _entities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Entity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _entities.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Entity entity)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _entities.Update(entity);
        await _context.SaveChangesAsync();
    }
}
