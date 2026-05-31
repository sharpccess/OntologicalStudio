using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class UniverseRepository : IUniverseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Universe> _universes;

    public UniverseRepository(ApplicationDbContext context)
    {
        _context = context;
        _universes = context.Set<Universe>();
    }

    public async Task<Universe> GetByIdAsync(Guid id)
    {
        return await _universes
            .Include(u => u.Entities)
            .Include(u => u.Scenarios)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<IEnumerable<Universe>> GetAllAsync()
    {
        return await _universes
            .Include(u => u.Entities)
            .Include(u => u.Scenarios)
            .Where(u => !u.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(Universe universe)
    {
        if (universe.Id == Guid.Empty)
            universe.Id = Guid.NewGuid();
        universe.CreatedAt = DateTime.UtcNow;
        await _universes.AddAsync(universe);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Universe universe)
    {
        universe.UpdatedAt = DateTime.UtcNow;
        universe.Entities = new List<Entity>();
        universe.Scenarios = new List<Scenario>();
        _universes.Update(universe);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Universe universe)
    {
        var originalName = universe.Name?.Trim() ?? "universe";
        universe.IsDeleted = true;
        universe.Name = $"{originalName}__deleted__{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        universe.UpdatedAt = DateTime.UtcNow;
        _universes.Update(universe);
        await _context.SaveChangesAsync();
    }
}
