using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ILocalizationService _localization;

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

    public bool HasSelectedUniverse => _universes.SelectedUniverse is not null;

    public event Action? EntitiesChanged;

    public EntitiesViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _localization = provider.GetRequiredService<ILocalizationService>();
        _localization.OnLanguageChanged += HandleLanguageChanged;
        _universes.SelectionChanged += async () =>
        {
            OnPropertyChanged(nameof(HasSelectedUniverse));
            await LoadAsync();
        };
        _universes.UniversesChanged += async () =>
        {
            OnPropertyChanged(nameof(HasSelectedUniverse));
            await LoadAsync();
        };
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
                EntityTypes.Add(TypeLocalizationHelper.Localize(t, _localization));
            NewEntityType ??= EntityTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error cargando tipos de entidad: {ex.Message}"
                : $"EntityTypes load failed: {ex.Message}";
        }
    }

    public async Task LoadAsync()
    {
        var universe = _universes.SelectedUniverse;
        Items.Clear();
        if (universe is null)
        {
            SelectedEntity = null;
            StatusMessage = _localization.CurrentLanguageCode == "es" ? "Selecciona primero un universo." : "Select a universe first.";
            EntitiesChanged?.Invoke();
            return;
        }
        try
        {
            var data = await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider, s => s.GetByUniverseAsync(universe.Id));
            foreach (var entity in data.Where(x => x.EntityType is not null))
                TypeLocalizationHelper.Localize(entity.EntityType!, _localization);
            foreach (var e in data.OrderBy(e => e.Name))
                Items.Add(e);
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"{Items.Count} entidad(es) en '{universe.Name}'."
                : $"{Items.Count} entity/entities in '{universe.Name}'.";
            EntitiesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error: {ex.Message}"
                : $"Error: {ex.Message}";
        }
    }

    private void HandleLanguageChanged()
    {
        _ = InitAsync();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null) { StatusMessage = _localization.CurrentLanguageCode == "es" ? "Selecciona primero un universo." : "Select a universe first."; return; }
        if (string.IsNullOrWhiteSpace(NewName)) { StatusMessage = _localization.CurrentLanguageCode == "es" ? "El nombre es obligatorio." : "Name is required."; return; }
        if (NewEntityType is null) { StatusMessage = _localization.CurrentLanguageCode == "es" ? "El tipo de entidad es obligatorio." : "Entity type is required."; return; }

        try
        {
            await ScopedRunner.RunAsync<IEntityService>(_provider,
                s => s.CreateAsync(NewName.Trim(), NewDescription?.Trim() ?? string.Empty, NewEntityType.Id, universe.Id));
            NewName = string.Empty;
            NewDescription = string.Empty;
            await LoadAsync();
            _universes.NotifyDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error al crear: {ex.Message}"
                : $"Create failed: {ex.Message}";
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
            _universes.NotifyDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error al eliminar: {ex.Message}"
                : $"Delete failed: {ex.Message}";
        }
    }
}
