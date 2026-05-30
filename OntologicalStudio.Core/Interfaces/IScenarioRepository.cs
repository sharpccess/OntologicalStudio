using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IScenarioRepository
{
    Task<Scenario> GetByIdAsync(Guid id);
    Task<IEnumerable<Scenario>> GetByUniverseAsync(Guid universeId);
    Task AddAsync(Scenario scenario);
    Task UpdateAsync(Scenario scenario);
    Task DeleteAsync(Scenario scenario);
}
