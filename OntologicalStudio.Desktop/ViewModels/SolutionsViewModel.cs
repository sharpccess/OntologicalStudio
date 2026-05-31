using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class SolutionsViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;

    public ObservableCollection<Solution> Items { get; } = new();

    [ObservableProperty]
    private Scenario? currentScenario;

    [ObservableProperty]
    private Solution? selectedSolution;

    [ObservableProperty]
    private string extraInstructions = string.Empty;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string statusMessage = "Select a scenario to view its solutions.";

    public SolutionsViewModel(IServiceProvider provider)
    {
        _provider = provider;
    }

    partial void OnCurrentScenarioChanged(Scenario? value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        Items.Clear();
        SelectedSolution = null;
        if (CurrentScenario is null)
        {
            StatusMessage = "Select a scenario to view its solutions.";
            return;
        }
        try
        {
            var data = await ScopedRunner.RunAsync<ISolutionService, IEnumerable<Solution>>(
                _provider, s => s.GetByScenarioAsync(CurrentScenario.Id));
            foreach (var s in data)
                Items.Add(s);
            SelectedSolution = Items.FirstOrDefault();
            StatusMessage = $"{Items.Count} solution(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (CurrentScenario is null) { StatusMessage = "Select a scenario first."; return; }
        if (IsRunning) return;
        IsRunning = true;
        StatusMessage = "Running scenario against AI provider...";
        try
        {
            var sol = await ScopedRunner.RunAsync<ISolutionService, Solution>(
                _provider,
                s => s.RunAsync(CurrentScenario.Id, string.IsNullOrWhiteSpace(ExtraInstructions) ? null : ExtraInstructions));
            ExtraInstructions = string.Empty;
            await LoadAsync();
            SelectedSolution = Items.FirstOrDefault(x => x.Id == sol.Id);
            StatusMessage = $"Solution created with {sol.Artifacts.Count} artifact(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSolution is null) return;
        var id = SelectedSolution.Id;
        try
        {
            await ScopedRunner.RunAsync<ISolutionService>(_provider, s => s.DeleteAsync(id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MarkFinalAsync()
    {
        if (SelectedSolution is null) return;
        var id = SelectedSolution.Id;
        try
        {
            await ScopedRunner.RunAsync<ISolutionService>(_provider, s => s.UpdateStatusAsync(id, SolutionStatus.Final));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update failed: {ex.Message}";
        }
    }
}
