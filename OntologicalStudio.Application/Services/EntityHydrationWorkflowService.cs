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

    public async Task<HydrationPreview> PreviewHydrationAsync(Guid entityId, HydrationOptions options, string? customPrompt = null, string languageCode = "en")
    {
        var entity = await _entities.GetByIdAsync(entityId)
            ?? throw new InvalidOperationException("Entity not found.");

        var result = await _hydrationService.HydrateEntityAsync(entityId, options, customPrompt, languageCode);
        result.EntityId = entityId;

        return new HydrationPreview
        {
            EntityId = entityId,
            PromptUsed = string.IsNullOrWhiteSpace(result.PromptUsed)
                ? BuildPrompt(entity, options, customPrompt, languageCode)
                : result.PromptUsed,
            ProviderUsed = string.IsNullOrWhiteSpace(result.ProviderUsed)
                ? "ConfigurableAIProvider"
                : result.ProviderUsed,
            CurrentHydrationData = entity.HydrationData,
            CurrentNotes = entity.Notes,
            CurrentConfidenceLevel = entity.ConfidenceLevel,
            CurrentCompletenessScore = entity.CompletenessScore,
            Result = result
        };
    }

    public async Task<HydrationLog> ApplyHydrationAsync(Guid entityId, HydrationApplyRequest request)
    {
        var entity = await _entities.GetByIdAsync(entityId)
            ?? throw new InvalidOperationException("Entity not found.");

        var appliedFields = new List<string>();

        if (request.ApplyHydrationData && !string.IsNullOrWhiteSpace(request.Preview.SuggestedProperties))
        {
            entity.HydrationData = request.Preview.SuggestedProperties;
            appliedFields.Add(nameof(Entity.HydrationData));
        }

        if (request.ApplyNotes && !string.IsNullOrWhiteSpace(request.Preview.SuggestedNotes))
        {
            entity.Description = string.Join(Environment.NewLine + Environment.NewLine,
                new[] { entity.Description, request.Preview.SuggestedNotes }.Where(x => !string.IsNullOrWhiteSpace(x)));
            appliedFields.Add(nameof(Entity.Description));
        }

        if (request.ApplyConfidence)
        {
            entity.ConfidenceLevel = request.Preview.ConfidenceScore;
            appliedFields.Add(nameof(Entity.ConfidenceLevel));
        }

        if (request.ApplyCompleteness && request.Preview.CompletenessScore > 0)
        {
            entity.CompletenessScore = request.Preview.CompletenessScore;
            appliedFields.Add(nameof(Entity.CompletenessScore));
        }

        await _entities.UpdateAsync(entity);

        var log = new HydrationLog
        {
            EntityId = entityId,
            PromptUsed = request.PromptUsed,
            ProviderUsed = request.ProviderUsed,
            RawResponse = request.Preview.AnalysisNotes?.Length > 0
                ? request.Preview.AnalysisNotes
                : $"{request.Preview.SuggestedProperties}{Environment.NewLine}{request.Preview.SuggestedNotes}",
            AppliedFields = System.Text.Json.JsonSerializer.Serialize(appliedFields)
        };

        await _logs.AddAsync(log);
        return log;
    }

    public Task<IEnumerable<HydrationLog>> GetHistoryAsync(Guid entityId) => _logs.GetByEntityAsync(entityId);

    private static string BuildPrompt(Entity entity, HydrationOptions options, string? customPrompt, string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
            return customPrompt;

        var requested = new List<string>();
        if (options.IncludePersonalities) requested.Add("personality");
        if (options.IncludeMotivations) requested.Add("motivations");
        if (options.IncludeFears) requested.Add("fears");
        if (options.IncludeIncentives) requested.Add("incentives");
        if (options.IncludeBehavioralPatterns) requested.Add("behavioral patterns");

        return languageCode == "es"
            ? $"Hidrata la entidad '{entity.Name}' de tipo '{entity.EntityType?.Name ?? "Entidad"}' usando: {string.Join(", ", requested)}. Descripción: {entity.Description}. Notas existentes: {entity.Notes}"
            : $"Hydrate entity '{entity.Name}' of type '{entity.EntityType?.Name ?? "Entity"}' using: {string.Join(", ", requested)}. Description: {entity.Description}. Existing notes: {entity.Notes}";
    }
}