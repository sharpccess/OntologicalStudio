using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class RelationshipRepository : IRelationshipRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Relationship> _relationships;

    public RelationshipRepository(ApplicationDbContext context)
    {
        _context = context;
        _relationships = context.Set<Relationship>();
    }

    public async Task<Relationship> GetByIdAsync(Guid id)
    {
        return await _relationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Include(r => r.RelationshipType)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<IEnumerable<Relationship>> GetBySourceEntityAsync(Guid entityId)
    {
        return await _relationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Include(r => r.RelationshipType)
            .Where(r => r.SourceEntityId == entityId && !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Relationship>> GetByTargetEntityAsync(Guid entityId)
    {
        return await _relationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .Include(r => r.RelationshipType)
            .Where(r => r.TargetEntityId == entityId && !r.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(Relationship relationship)
    {
        if (relationship.Id == Guid.Empty)
            relationship.Id = Guid.NewGuid();
        relationship.CreatedAt = DateTime.UtcNow;
        relationship.SourceEntity = null!;
        relationship.TargetEntity = null!;
        relationship.RelationshipType = null!;
        await _relationships.AddAsync(relationship);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Relationship relationship)
    {
        relationship.UpdatedAt = DateTime.UtcNow;
        relationship.SourceEntity = null!;
        relationship.TargetEntity = null!;
        relationship.RelationshipType = null!;
        _relationships.Update(relationship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Relationship relationship)
    {
        relationship.IsDeleted = true;
        relationship.UpdatedAt = DateTime.UtcNow;
        _relationships.Update(relationship);
        await _context.SaveChangesAsync();
    }
}
