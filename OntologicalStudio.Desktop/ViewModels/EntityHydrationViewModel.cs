using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using System.Collections.ObjectModel;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class EntityHydrationViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;

    public ObservableCollection<HydrationLog> History { get; } = new();

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
        _ = LoadHistoryAsync();
    }

    public EntityHydrationViewModel(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task PreviewCurrentNodeAsync()
    {
        await PreviewAsync();
    }

    public async Task<bool> HydrateCurrentNodeAsync(string? customPrompt = null)
    {
        if (SelectedNode is null || IsBusy)
            return false;

        CustomPrompt = customPrompt?.Trim() ?? string.Empty;
        await PreviewAsync();
        if (Preview is null)
            return false;

        await ApplyAsync();
        return Preview is not null;
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
        StatusMessage = "Generating hydration preview...";
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
                        DetailLevel = 2,
                        MaxSuggestions = 8
                    },
                    CustomPrompt));
            BuildDiff();
            StatusMessage = "Preview ready. Review and apply if useful.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hydration preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (SelectedNode is null || Preview is null || IsBusy)
            return;

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
            StatusMessage = "Hydration applied to entity.";
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
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
}