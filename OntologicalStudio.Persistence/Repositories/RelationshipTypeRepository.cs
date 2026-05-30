using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class RelationshipTypeRepository : IRelationshipTypeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<RelationshipType> _relationshipTypes;

    public RelationshipTypeRepository(ApplicationDbContext context)
    {
        _context = context;
        _relationshipTypes = context.Set<RelationshipType>();
    }

    public async Task<RelationshipType> GetByIdAsync(Guid id)
    {
        return await _relationshipTypes.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<IEnumerable<RelationshipType>> GetAllAsync()
    {
        return await _relationshipTypes
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(RelationshipType relationshipType)
    {
        relationshipType.Id = Guid.NewGuid();
        relationshipType.CreatedAt = DateTime.UtcNow;
        await _relationshipTypes.AddAsync(relationshipType);
    }

    public async Task UpdateAsync(RelationshipType relationshipType)
    {
        relationshipType.UpdatedAt = DateTime.UtcNow;
        _relationshipTypes.Update(relationshipType);
    }

    public async Task DeleteAsync(RelationshipType relationshipType)
    {
        relationshipType.IsDeleted = true;
        relationshipType.UpdatedAt = DateTime.UtcNow;
        _relationshipTypes.Update(relationshipType);
    }
}
