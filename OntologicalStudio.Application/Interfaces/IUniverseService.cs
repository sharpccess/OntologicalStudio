using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IUniverseService
{
    Task<Universe> GetByIdAsync(Guid id);
    Task<IEnumerable<Universe>> GetAllAsync();
    Task<Universe> CreateAsync(string name, string description, bool isPublic = false);
    Task UpdateAsync(Universe universe);
    Task DeleteAsync(Guid id);
}
