using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class ScenarioRepository : IScenarioRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Scenario> _scenarios;

    public ScenarioRepository(ApplicationDbContext context)
    {
        _context = context;
        _scenarios = context.Set<Scenario>();
    }

    public async Task<Scenario> GetByIdAsync(Guid id)
    {
        return await _scenarios
            .Include(s => s.Universe)
            .Include(s => s.Entities)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<IEnumerable<Scenario>> GetByUniverseAsync(Guid universeId)
    {
        return await _scenarios
            .Include(s => s.Universe)
            .Include(s => s.Entities)
            .Where(s => s.UniverseId == universeId && !s.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(Scenario scenario)
    {
        scenario.Id = Guid.NewGuid();
        scenario.CreatedAt = DateTime.UtcNow;
        await _scenarios.AddAsync(scenario);
    }

    public async Task UpdateAsync(Scenario scenario)
    {
        scenario.UpdatedAt = DateTime.UtcNow;
        _scenarios.Update(scenario);
    }

    public async Task DeleteAsync(Scenario scenario)
    {
        scenario.IsDeleted = true;
        scenario.UpdatedAt = DateTime.UtcNow;
        _scenarios.Update(scenario);
    }
}
