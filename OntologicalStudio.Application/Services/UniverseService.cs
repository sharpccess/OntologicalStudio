using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class UniverseService : IUniverseService
{
    private readonly IUniverseRepository _universeRepository;

    public UniverseService(IUniverseRepository universeRepository)
    {
        _universeRepository = universeRepository;
    }

    public async Task<Universe> GetByIdAsync(Guid id)
    {
        return await _universeRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Universe>> GetAllAsync()
    {
        return await _universeRepository.GetAllAsync();
    }

    public async Task<Universe> CreateAsync(string name, string description, bool isPublic = false)
    {
        var universe = new Universe
        {
            Name = name,
            Description = description,
            IsPublic = isPublic
        };
        await _universeRepository.AddAsync(universe);
        return universe;
    }

    public async Task UpdateAsync(Universe universe)
    {
        await _universeRepository.UpdateAsync(universe);
    }

    public async Task DeleteAsync(Guid id)
    {
        var universe = await _universeRepository.GetByIdAsync(id);
        if (universe != null)
        {
            await _universeRepository.DeleteAsync(universe);
        }
    }
}
