using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IEntityHydrationWorkflowService
{
    Task<HydrationResult> PreviewHydrationAsync(Guid entityId, HydrationOptions options);
    Task<HydrationLog> ApplyHydrationAsync(Guid entityId, HydrationResult preview, string promptUsed, string providerUsed);
    Task<IEnumerable<HydrationLog>> GetHistoryAsync(Guid entityId);
}