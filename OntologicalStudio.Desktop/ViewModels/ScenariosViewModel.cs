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

public partial class ScenariosViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;

    public ObservableCollection<Scenario> Items { get; } = new();

    public SolutionsViewModel Solutions { get; }

    [ObservableProperty]
    private Scenario? selectedScenario;

    partial void OnSelectedScenarioChanged(Scenario? value)
    {
        Solutions.CurrentScenario = value;
    }

    [ObservableProperty]
    private string newTitle = string.Empty;

    [ObservableProperty]
    private string newDescription = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public ScenariosViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        Solutions = new SolutionsViewModel(provider);
        _universes.SelectionChanged += async () => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        Items.Clear();
        var u = _universes.SelectedUniverse;
        if (u is null) { StatusMessage = "Select a universe first."; return; }
        try
        {
            var data = await ScopedRunner.RunAsync<IScenarioService, IEnumerable<Scenario>>(
                _provider, s => s.GetByUniverseAsync(u.Id));
            foreach (var s in data.OrderByDescending(s => s.CreatedAt))
                Items.Add(s);
            StatusMessage = $"{Items.Count} scenario(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var u = _universes.SelectedUniverse;
        if (u is null) { StatusMessage = "Select a universe first."; return; }
        if (string.IsNullOrWhiteSpace(NewTitle)) { StatusMessage = "Title is required."; return; }
        try
        {
            await ScopedRunner.RunAsync<IScenarioService>(_provider,
                s => s.CreateAsync(NewTitle.Trim(), NewDescription?.Trim() ?? string.Empty, u.Id));
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedScenario is null) return;
        var id = SelectedScenario.Id;
        try
        {
            await ScopedRunner.RunAsync<IScenarioService>(_provider, s => s.DeleteAsync(id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }
}
