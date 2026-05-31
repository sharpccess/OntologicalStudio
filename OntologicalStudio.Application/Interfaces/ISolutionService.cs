using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface ISolutionService
{
    Task<Solution?> GetByIdAsync(Guid id);
    Task<IEnumerable<Solution>> GetByScenarioAsync(Guid scenarioId);
    Task<Solution> RunAsync(Guid scenarioId, string? extraInstructions, CancellationToken ct = default);
    Task DeleteAsync(Guid id);
    Task UpdateRatingAsync(Guid id, int rating);
    Task UpdateNotesAsync(Guid id, string notes);
    Task UpdateStatusAsync(Guid id, SolutionStatus status);
}
