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

public partial class RelationshipsViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;
    private readonly ILocalizationService _localization;

    public ObservableCollection<RelationshipRow> Items { get; } = new();
    public ObservableCollection<Entity> UniverseEntities { get; } = new();
    public ObservableCollection<RelationshipType> RelationshipTypes { get; } = new();

    [ObservableProperty]
    private RelationshipRow? selectedRelationship;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public RelationshipsViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _localization = provider.GetRequiredService<ILocalizationService>();
        _localization.OnLanguageChanged += HandleLanguageChanged;
        _universes.SelectionChanged += async () => await ReloadForUniverseAsync();
        _universes.UniversesChanged += async () => await ReloadForUniverseAsync();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await ReloadForUniverseAsync();
    }

    public async Task ReloadForUniverseAsync()
    {
        UniverseEntities.Clear();
        Items.Clear();
        var u = _universes.SelectedUniverse;
        if (u is null) { StatusMessage = _localization.CurrentLanguageCode == "es" ? "Selecciona primero un universo." : "Select a universe first."; return; }

        try
        {
            var entities = (await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider, s => s.GetByUniverseAsync(u.Id))).ToList();
            foreach (var e in entities.OrderBy(e => e.Name))
                UniverseEntities.Add(e);

            var entityIds = entities.Select(e => e.Id).ToHashSet();
            var seen = new HashSet<Guid>();
            foreach (var e in entities)
            {
                var rels = await ScopedRunner.RunAsync<IRelationshipService, IEnumerable<Relationship>>(
                    _provider, s => s.GetBySourceEntityAsync(e.Id));
                foreach (var r in rels)
                {
                    if (!seen.Add(r.Id)) continue;
                    if (!entityIds.Contains(r.TargetEntityId)) continue;
                    var relationshipTypeName = r.RelationshipType?.Name ?? "?";
                    Items.Add(new RelationshipRow(
                        r.Id,
                        r.SourceEntity?.Name ?? entities.FirstOrDefault(x => x.Id == r.SourceEntityId)?.Name ?? "?",
                        TypeLocalizationHelper.LocalizeRelationshipTypeName(relationshipTypeName, _localization),
                        r.TargetEntity?.Name ?? entities.FirstOrDefault(x => x.Id == r.TargetEntityId)?.Name ?? "?",
                        r.Description));
                }
            }
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"{Items.Count} relación(es)."
                : $"{Items.Count} relationship(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error: {ex.Message}"
                : $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await ReloadForUniverseAsync();

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedRelationship is null) return;
        var id = SelectedRelationship.Id;
        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(_provider, s => s.DeleteAsync(id));
            await ReloadForUniverseAsync();
            _universes.NotifyDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Error al eliminar: {ex.Message}"
                : $"Delete failed: {ex.Message}";
        }
    }

    private void HandleLanguageChanged()
    {
        _ = ReloadForUniverseAsync();
    }
}

public record RelationshipRow(Guid Id, string Source, string Type, string Target, string Description)
{
    public string Display => $"{Source}  ──{Type}──>  {Target}";
}
