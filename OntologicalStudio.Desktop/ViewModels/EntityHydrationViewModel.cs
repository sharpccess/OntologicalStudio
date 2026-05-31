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
    private HydrationResult? preview;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Select a node and preview hydration.";

    partial void OnSelectedNodeChanged(CanvasEntityNodeViewModel? value)
    {
        Preview = null;
        _ = LoadHistoryAsync();
    }

    public EntityHydrationViewModel(IServiceProvider provider)
    {
        _provider = provider;
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
            Preview = await ScopedRunner.RunAsync<IEntityHydrationWorkflowService, HydrationResult>(
                _provider,
                service => service.PreviewHydrationAsync(SelectedNode.Id, new HydrationOptions
                {
                    IncludeMotivations = IncludeMotivations,
                    IncludeFears = IncludeFears,
                    IncludeIncentives = IncludeIncentives,
                    IncludeBehavioralPatterns = IncludeBehavioralPatterns,
                    IncludePersonalities = true,
                    DetailLevel = 2,
                    MaxSuggestions = 8
                }));
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
                service => service.ApplyHydrationAsync(
                    SelectedNode.Id,
                    Preview,
                    $"Hydrate entity '{SelectedNode.Name}' with motivations={IncludeMotivations}, fears={IncludeFears}, incentives={IncludeIncentives}, patterns={IncludeBehavioralPatterns}",
                    "ConfigurableAIProvider"));
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
}