using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IUniverseRepository
{
    Task<Universe> GetByIdAsync(Guid id);
    Task<IEnumerable<Universe>> GetAllAsync();
    Task AddAsync(Universe universe);
    Task UpdateAsync(Universe universe);
    Task DeleteAsync(Universe universe);
}
