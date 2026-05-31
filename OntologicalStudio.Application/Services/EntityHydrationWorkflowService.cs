using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public class EntityHydrationWorkflowService : IEntityHydrationWorkflowService
{
    private readonly IAIHydrationService _hydrationService;
    private readonly IEntityRepository _entities;
    private readonly IHydrationLogRepository _logs;

    public EntityHydrationWorkflowService(
        IAIHydrationService hydrationService,
        IEntityRepository entities,
        IHydrationLogRepository logs)
    {
        _hydrationService = hydrationService;
        _entities = entities;
        _logs = logs;
    }

    public Task<HydrationResult> PreviewHydrationAsync(Guid entityId, HydrationOptions options) =>
        _hydrationService.HydrateEntityAsync(entityId, options);

    public async Task<HydrationLog> ApplyHydrationAsync(Guid entityId, HydrationResult preview, string promptUsed, string providerUsed)
    {
        var entity = await _entities.GetByIdAsync(entityId)
            ?? throw new InvalidOperationException("Entity not found.");

        entity.HydrationData = string.IsNullOrWhiteSpace(preview.SuggestedProperties)
            ? entity.HydrationData
            : preview.SuggestedProperties;
        entity.Notes = string.IsNullOrWhiteSpace(preview.SuggestedNotes)
            ? entity.Notes
            : string.Join(Environment.NewLine + Environment.NewLine,
                new[] { entity.Notes, preview.SuggestedNotes }.Where(x => !string.IsNullOrWhiteSpace(x)));
        entity.ConfidenceLevel = preview.ConfidenceScore;
        entity.CompletenessScore = preview.CompletenessScore > 0
            ? preview.CompletenessScore
            : entity.CompletenessScore;

        await _entities.UpdateAsync(entity);

        var log = new HydrationLog
        {
            EntityId = entityId,
            PromptUsed = promptUsed,
            ProviderUsed = providerUsed,
            RawResponse = preview.AnalysisNotes?.Length > 0
                ? preview.AnalysisNotes
                : $"{preview.SuggestedProperties}{Environment.NewLine}{preview.SuggestedNotes}",
            AppliedFields = "[\"HydrationData\",\"Notes\",\"ConfidenceLevel\",\"CompletenessScore\"]"
        };

        await _logs.AddAsync(log);
        return log;
    }

    public Task<IEnumerable<HydrationLog>> GetHistoryAsync(Guid entityId) => _logs.GetByEntityAsync(entityId);
}