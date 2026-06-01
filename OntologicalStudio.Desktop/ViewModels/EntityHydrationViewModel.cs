using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using System.Collections.ObjectModel;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class EntityHydrationViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly ILocalizationService _localization;

    public ObservableCollection<HydrationLog> History { get; } = new();
    public ObservableCollection<HydrationModeOption> HydrationModes { get; } = new();

    [ObservableProperty]
    private CanvasEntityNodeViewModel? selectedNode;

    [ObservableProperty]
    private bool includeMotivations = true;

    [ObservableProperty]
    private bool includeFears = true;

    [ObservableProperty]
    private bool includeIncentives = true;

    [ObservableProperty]
    private bool includeBehavioralPatterns = true;

    [ObservableProperty]
    private HydrationPreview? preview;

    [ObservableProperty]
    private string customPrompt = string.Empty;

    [ObservableProperty]
    private HydrationModeOption? selectedHydrationMode;

    [ObservableProperty]
    private string lastPromptSent = string.Empty;

    [ObservableProperty]
    private bool applyHydrationData = true;

    [ObservableProperty]
    private bool applyNotes = true;

    [ObservableProperty]
    private bool applyConfidence = true;

    [ObservableProperty]
    private bool applyCompleteness = true;

    [ObservableProperty]
    private string diffHydrationData = string.Empty;

    [ObservableProperty]
    private string diffNotes = string.Empty;

    [ObservableProperty]
    private string diffScores = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Select a node and preview hydration.";

    partial void OnSelectedNodeChanged(CanvasEntityNodeViewModel? value)
    {
        Preview = null;
        DiffHydrationData = string.Empty;
        DiffNotes = string.Empty;
        DiffScores = string.Empty;
        CustomPrompt = string.Empty;
        LastPromptSent = string.Empty;
        _ = LoadHistoryAsync();
    }

    public EntityHydrationViewModel(IServiceProvider provider)
    {
        _provider = provider;
        _localization = provider.GetRequiredService<ILocalizationService>();
        _localization.OnLanguageChanged += HandleLanguageChanged;
        RebuildHydrationModes();
    }

    public async Task PreviewCurrentNodeAsync()
    {
        await PreviewAsync();
    }

    public async Task<bool> HydrateCurrentNodeAsync(string? customPrompt = null, bool forceApplyDescription = false)
    {
        if (SelectedNode is null || IsBusy)
            return false;

        CustomPrompt = customPrompt?.Trim() ?? string.Empty;
        await PreviewAsync();
        if (Preview is null)
            return false;

        var previousApplyNotes = ApplyNotes;
        if (forceApplyDescription)
            ApplyNotes = true;

        try
        {
            return await ApplyCurrentPreviewAsync();
        }
        finally
        {
            if (forceApplyDescription)
                ApplyNotes = previousApplyNotes;
        }
    }

    [RelayCommand]
    private async Task HydrateCurrentNode()
    {
        await HydrateCurrentNodeAsync(CustomPrompt);
    }

    public async Task LoadHistoryAsync()
    {
        History.Clear();
        if (SelectedNode is null)
            return;

        var items = await ScopedRunner.RunAsync<IEntityHydrationWorkflowService, IEnumerable<HydrationLog>>(
            _provider,
            service => service.GetHistoryAsync(SelectedNode.Id));

        foreach (var item in items)
            History.Add(item);
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (SelectedNode is null || IsBusy)
            return;

        IsBusy = true;
        var customPrompt = string.IsNullOrWhiteSpace(CustomPrompt)
            ? null
            : CustomPrompt.Trim();

        var statusService = _provider.GetService(typeof(OntologicalStudio.Core.Interfaces.IAiOperationStatusService))
            as OntologicalStudio.Core.Interfaces.IAiOperationStatusService;
        var settings = _provider.GetService(typeof(OntologicalStudio.Core.Interfaces.IAiConnectionSettingsService))
            as OntologicalStudio.Core.Interfaces.IAiConnectionSettingsService;
        var providerLabel = "AI";
        if (settings is not null)
        {
            var s = await settings.GetAsync();
            providerLabel = $"{s.Provider} ({s.Model})";
        }
        var operationTitle = _localization.CurrentLanguageCode == "es"
            ? $"Hidratando '{SelectedNode.Entity.Name}'…"
            : $"Hydrating '{SelectedNode.Entity.Name}'…";
        statusService?.Begin(operationTitle, providerLabel);

        StatusMessage = _localization.CurrentLanguageCode == "es"
            ? "Generando hidratación con investigación web..."
            : "Generating hydration with web research...";
        try
        {
            Preview = await ScopedRunner.RunAsync<IEntityHydrationWorkflowService, HydrationPreview>(
                _provider,
                service => service.PreviewHydrationAsync(
                    SelectedNode.Id,
                    new HydrationOptions
                    {
                        IncludeMotivations = IncludeMotivations,
                        IncludeFears = IncludeFears,
                        IncludeIncentives = IncludeIncentives,
                        IncludeBehavioralPatterns = IncludeBehavioralPatterns,
                        IncludePersonalities = true,
                        HydrationMode = SelectedHydrationMode?.Key ?? "factual",
                        DetailLevel = 2,
                        MaxSuggestions = 8
                    },
                    customPrompt,
                    _localization.CurrentLanguageCode));
            LastPromptSent = Preview.PromptUsed;
            BuildDiff();
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? "Hidratación lista. Se aplicará sobre la entidad."
                : "Hydration ready. It will be applied to the entity.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? "Hidratación cancelada por el usuario."
                : "Hydration cancelled by the user.";
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Falló la hidratación: {ex.Message}"
                : $"Hydration failed: {ex.Message}";
        }
        finally
        {
            statusService?.End();
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        await ApplyCurrentPreviewAsync();
    }

    private async Task<bool> ApplyCurrentPreviewAsync()
    {
        if (SelectedNode is null || Preview is null || IsBusy)
            return false;

        IsBusy = true;
        try
        {
            await ScopedRunner.RunAsync<IEntityHydrationWorkflowService, HydrationLog>(
                _provider,
                service => service.ApplyHydrationAsync(SelectedNode.Id, new HydrationApplyRequest
                {
                    EntityId = SelectedNode.Id,
                    PromptUsed = Preview.PromptUsed,
                    ProviderUsed = Preview.ProviderUsed,
                    Preview = Preview.Result,
                    ApplyHydrationData = ApplyHydrationData,
                    ApplyNotes = ApplyNotes,
                    ApplyConfidence = ApplyConfidence,
                    ApplyCompleteness = ApplyCompleteness
                }));

            if (ApplyNotes && !string.IsNullOrWhiteSpace(Preview.Result.SuggestedNotes))
            {
                SelectedNode.Entity.Description = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    new[] { SelectedNode.Entity.Description, Preview.Result.SuggestedNotes }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            if (ApplyHydrationData && !string.IsNullOrWhiteSpace(Preview.Result.SuggestedProperties))
                SelectedNode.Entity.HydrationData = Preview.Result.SuggestedProperties;

            if (ApplyConfidence)
                SelectedNode.Entity.ConfidenceLevel = Preview.Result.ConfidenceScore;

            if (ApplyCompleteness && Preview.Result.CompletenessScore > 0)
                SelectedNode.Entity.CompletenessScore = Preview.Result.CompletenessScore;

            SelectedNode.RefreshDisplay();
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? "Hidratación aplicada a la entidad."
                : "Hydration applied to entity.";
            await LoadHistoryAsync();
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Falló la aplicación: {ex.Message}"
                : $"Apply failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildDiff()
    {
        if (Preview is null)
        {
            DiffHydrationData = string.Empty;
            DiffNotes = string.Empty;
            DiffScores = string.Empty;
            return;
        }

        DiffHydrationData =
            $"Current:{Environment.NewLine}{Preview.CurrentHydrationData}{Environment.NewLine}{Environment.NewLine}" +
            $"Suggested:{Environment.NewLine}{Preview.Result.SuggestedProperties}";
        DiffNotes =
            $"Current:{Environment.NewLine}{Preview.CurrentNotes}{Environment.NewLine}{Environment.NewLine}" +
            $"Suggested:{Environment.NewLine}{Preview.Result.SuggestedNotes}";
        DiffScores =
            $"Confidence: {Preview.CurrentConfidenceLevel} -> {Preview.Result.ConfidenceScore}{Environment.NewLine}" +
            $"Completeness: {Preview.CurrentCompletenessScore} -> {Preview.Result.CompletenessScore}";
    }

    private void HandleLanguageChanged()
    {
        RebuildHydrationModes();
    }

    private void RebuildHydrationModes()
    {
        var previousKey = SelectedHydrationMode?.Key;
        HydrationModes.Clear();

        if (_localization.CurrentLanguageCode == "es")
        {
            HydrationModes.Add(new HydrationModeOption("factual", "Factual", "Modo factual", "Usa un tono factual y concreto. Evita frases vagas o poéticas. Prioriza hechos observables, rasgos claros y atributos reutilizables."));
            HydrationModes.Add(new HydrationModeOption("psychological", "Psychological", "Modo psicológico", "Enfoca la hidratación en motivaciones, creencias, miedos, contradicciones internas y patrones de comportamiento."));
            HydrationModes.Add(new HydrationModeOption("organizational", "Organizational", "Modo organizacional", "Enfoca la hidratación en rol, incentivos, dependencias, conflictos, poder, estructura y contexto organizacional."));
            HydrationModes.Add(new HydrationModeOption("strategic", "Strategic", "Modo estratégico", "Enfoca la hidratación en objetivos, riesgos, palancas, restricciones, oportunidades y efectos sistémicos."));
        }
        else
        {
            HydrationModes.Add(new HydrationModeOption("factual", "Factual mode", "Factual mode", "Use a factual and concrete tone. Avoid vague or poetic phrasing. Prioritize observable facts, clear traits, and reusable attributes."));
            HydrationModes.Add(new HydrationModeOption("psychological", "Psychological mode", "Psychological mode", "Focus the hydration on motivations, beliefs, fears, internal contradictions, and behavioral patterns."));
            HydrationModes.Add(new HydrationModeOption("organizational", "Organizational mode", "Organizational mode", "Focus the hydration on role, incentives, dependencies, conflicts, power, structure, and organizational context."));
            HydrationModes.Add(new HydrationModeOption("strategic", "Strategic mode", "Strategic mode", "Focus the hydration on goals, risks, leverage points, constraints, opportunities, and systemic effects."));
        }

        SelectedHydrationMode = HydrationModes.FirstOrDefault(x => x.Key == previousKey) ?? HydrationModes.FirstOrDefault();
    }
}

public class HydrationModeOption
{
    public HydrationModeOption(string key, string name, string displayName, string instruction)
    {
        Key = key;
        Name = name;
        DisplayName = displayName;
        Instruction = instruction;
    }

    public string Key { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string Instruction { get; }
}