using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class HydrationLogRepository : IHydrationLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<HydrationLog> _logs;

    public HydrationLogRepository(ApplicationDbContext context)
    {
        _context = context;
        _logs = context.Set<HydrationLog>();
    }

    public async Task<HydrationLog?> GetByIdAsync(Guid id) =>
        await _logs.Include(x => x.Entity).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

    public async Task<IEnumerable<HydrationLog>> GetByEntityAsync(Guid entityId) =>
        await _logs.Where(x => x.EntityId == entityId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(HydrationLog log)
    {
        if (log.Id == Guid.Empty)
            log.Id = Guid.NewGuid();
        log.CreatedAt = DateTime.UtcNow;
        log.Entity = null;
        await _logs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(HydrationLog log)
    {
        log.IsDeleted = true;
        log.UpdatedAt = DateTime.UtcNow;
        _logs.Update(log);
        await _context.SaveChangesAsync();
    }
}