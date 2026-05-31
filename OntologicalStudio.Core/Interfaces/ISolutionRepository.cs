using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface ISolutionRepository
{
    Task<Solution?> GetByIdAsync(Guid id);
    Task<IEnumerable<Solution>> GetByScenarioAsync(Guid scenarioId);
    Task AddAsync(Solution solution);
    Task UpdateAsync(Solution solution);
    Task DeleteAsync(Solution solution);
}
