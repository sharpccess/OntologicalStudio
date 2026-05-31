using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class UniversesViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly ILocalizationService _localization;

    public ObservableCollection<Universe> Items { get; } = new();

    [ObservableProperty]
    private Universe? selectedUniverse;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newDescription = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public event Action? UniversesChanged;
    public event Action? SelectionChanged;

    public UniversesViewModel(IServiceProvider provider)
    {
        _provider = provider;
        _localization = provider.GetRequiredService<ILocalizationService>();
    }

    partial void OnSelectedUniverseChanged(Universe? value) => SelectionChanged?.Invoke();

    public void NotifyDataChanged() => UniversesChanged?.Invoke();

    public async Task LoadAsync()
    {
        try
        {
            var data = await ScopedRunner.RunAsync<IUniverseService, System.Collections.Generic.IEnumerable<Universe>>(
                _provider, s => Task.Run(async () => await s.GetAllAsync()));
            var prevId = SelectedUniverse?.Id;
            Items.Clear();
            foreach (var u in data.OrderBy(u => u.Name))
                Items.Add(u);
            SelectedUniverse = prevId.HasValue ? Items.FirstOrDefault(x => x.Id == prevId) ?? Items.FirstOrDefault() : Items.FirstOrDefault();
            StatusMessage = $"{Items.Count} universe(s) loaded.";
            UniversesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            StatusMessage = _localization.CurrentLanguageCode == "es" ? "El nombre es obligatorio." : "Name is required.";
            return;
        }
        try
        {
            await ScopedRunner.RunAsync<IUniverseService>(_provider,
                s => s.CreateAsync(NewName.Trim(), NewDescription?.Trim() ?? string.Empty));
            NewName = string.Empty;
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
        if (SelectedUniverse is null) return;
        var id = SelectedUniverse.Id;
        try
        {
            await ScopedRunner.RunAsync<IUniverseService>(_provider, s => s.DeleteAsync(id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
