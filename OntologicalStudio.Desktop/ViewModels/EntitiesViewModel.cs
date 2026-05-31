using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class EntitiesViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;

    public ObservableCollection<Entity> Items { get; } = new();
    public ObservableCollection<EntityType> EntityTypes { get; } = new();

    [ObservableProperty]
    private Entity? selectedEntity;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newDescription = string.Empty;

    [ObservableProperty]
    private EntityType? newEntityType;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public event Action? EntitiesChanged;

    public EntitiesViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _universes.SelectionChanged += async () => await LoadAsync();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await LoadEntityTypesAsync();
        await LoadAsync();
    }

    private async Task LoadEntityTypesAsync()
    {
        try
        {
            var types = await ScopedRunner.RunAsync<IEntityTypeRepository, IEnumerable<EntityType>>(
                _provider, r => r.GetAllAsync());
            EntityTypes.Clear();
            foreach (var t in types.OrderBy(t => t.Name))
                EntityTypes.Add(t);
            NewEntityType ??= EntityTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"EntityTypes load failed: {ex.Message}";
        }
    }

    public async Task LoadAsync()
    {
        var universe = _universes.SelectedUniverse;
        Items.Clear();
        if (universe is null)
        {
            StatusMessage = "Select a universe first.";
            EntitiesChanged?.Invoke();
            return;
        }
        try
        {
            var data = await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider, s => s.GetByUniverseAsync(universe.Id));
            foreach (var e in data.OrderBy(e => e.Name))
                Items.Add(e);
            StatusMessage = $"{Items.Count} entity/entities in '{universe.Name}'.";
            EntitiesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null) { StatusMessage = "Select a universe first."; return; }
        if (string.IsNullOrWhiteSpace(NewName)) { StatusMessage = "Name is required."; return; }
        if (NewEntityType is null) { StatusMessage = "Entity type is required."; return; }

        try
        {
            await ScopedRunner.RunAsync<IEntityService>(_provider,
                s => s.CreateAsync(NewName.Trim(), NewDescription?.Trim() ?? string.Empty, NewEntityType.Id, universe.Id));
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
        if (SelectedEntity is null) return;
        var id = SelectedEntity.Id;
        try
        {
            await ScopedRunner.RunAsync<IEntityService>(_provider, s => s.DeleteAsync(id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }
}
