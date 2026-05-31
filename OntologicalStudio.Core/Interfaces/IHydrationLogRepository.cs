using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IHydrationLogRepository
{
    Task<HydrationLog?> GetByIdAsync(Guid id);
    Task<IEnumerable<HydrationLog>> GetByEntityAsync(Guid entityId);
    Task AddAsync(HydrationLog log);
    Task DeleteAsync(HydrationLog log);
}