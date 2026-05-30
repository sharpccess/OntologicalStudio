using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class ScenarioService : IScenarioService
{
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUniverseRepository _universeRepository;
    private readonly IEntityRepository _entityRepository;

    public ScenarioService(
        IScenarioRepository scenarioRepository,
        IUniverseRepository universeRepository,
        IEntityRepository entityRepository)
    {
        _scenarioRepository = scenarioRepository;
        _universeRepository = universeRepository;
        _entityRepository = entityRepository;
    }

    public async Task<Scenario> GetByIdAsync(Guid id)
    {
        return await _scenarioRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Scenario>> GetByUniverseAsync(Guid universeId)
    {
        return await _scenarioRepository.GetByUniverseAsync(universeId);
    }

    public async Task<Scenario> CreateAsync(string title, string description, Guid universeId)
    {
        var universe = await _universeRepository.GetByIdAsync(universeId);
        if (universe == null)
            throw new InvalidOperationException("Universe not found");

        var scenario = new Scenario
        {
            Title = title,
            Description = description,
            UniverseId = universeId,
            Status = ScenarioStatus.Draft
        };

        await _scenarioRepository.AddAsync(scenario);
        return scenario;
    }

    public async Task UpdateAsync(Scenario scenario)
    {
        await _scenarioRepository.UpdateAsync(scenario);
    }

    public async Task DeleteAsync(Guid id)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(id);
        if (scenario != null)
        {
            await _scenarioRepository.DeleteAsync(scenario);
        }
    }

    public async Task AddEntityToScenarioAsync(Guid scenarioId, Guid entityId, string? role = null)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);
        var entity = await _entityRepository.GetByIdAsync(entityId);

        if (scenario != null && entity != null && !scenario.Entities.Contains(entity))
        {
            scenario.Entities.Add(entity);
            await _scenarioRepository.UpdateAsync(scenario);
        }
    }
}
