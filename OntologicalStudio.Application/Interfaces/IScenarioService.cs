using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IScenarioService
{
    Task<Scenario> GetByIdAsync(Guid id);
    Task<IEnumerable<Scenario>> GetByUniverseAsync(Guid universeId);
    Task<Scenario> CreateAsync(string title, string description, Guid universeId);
    Task UpdateAsync(Scenario scenario);
    Task DeleteAsync(Guid id);
    Task AddEntityToScenarioAsync(Guid scenarioId, Guid entityId, string role = null);
}
