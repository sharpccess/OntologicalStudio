using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class SolutionRepository : ISolutionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Solution> _solutions;

    public SolutionRepository(ApplicationDbContext context)
    {
        _context = context;
        _solutions = context.Set<Solution>();
    }

    public async Task<Solution?> GetByIdAsync(Guid id) =>
        await _solutions
            .Include(s => s.Artifacts)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

    public async Task<IEnumerable<Solution>> GetByScenarioAsync(Guid scenarioId) =>
        await _solutions
            .Include(s => s.Artifacts)
            .Where(s => s.ScenarioId == scenarioId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Solution solution)
    {
        if (solution.Id == Guid.Empty)
            solution.Id = Guid.NewGuid();
        solution.CreatedAt = DateTime.UtcNow;
        foreach (var a in solution.Artifacts)
        {
            if (a.Id == Guid.Empty) a.Id = Guid.NewGuid();
            a.CreatedAt = DateTime.UtcNow;
            a.SolutionId = solution.Id;
        }
        await _solutions.AddAsync(solution);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Solution solution)
    {
        solution.UpdatedAt = DateTime.UtcNow;
        _solutions.Update(solution);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Solution solution)
    {
        solution.IsDeleted = true;
        solution.UpdatedAt = DateTime.UtcNow;
        _solutions.Update(solution);
        await _context.SaveChangesAsync();
    }
}
