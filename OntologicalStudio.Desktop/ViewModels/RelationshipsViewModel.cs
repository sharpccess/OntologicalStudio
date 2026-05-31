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

public partial class RelationshipsViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;

    public ObservableCollection<RelationshipRow> Items { get; } = new();
    public ObservableCollection<Entity> UniverseEntities { get; } = new();
    public ObservableCollection<RelationshipType> RelationshipTypes { get; } = new();

    [ObservableProperty]
    private Entity? sourceEntity;

    [ObservableProperty]
    private Entity? targetEntity;

    [ObservableProperty]
    private RelationshipType? relationshipType;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private RelationshipRow? selectedRelationship;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public RelationshipsViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _universes.SelectionChanged += async () => await ReloadForUniverseAsync();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await LoadRelationshipTypesAsync();
        await ReloadForUniverseAsync();
    }

    private async Task LoadRelationshipTypesAsync()
    {
        try
        {
            var rts = await ScopedRunner.RunAsync<IRelationshipTypeRepository, IEnumerable<RelationshipType>>(
                _provider, r => r.GetAllAsync());
            RelationshipTypes.Clear();
            foreach (var t in rts.OrderBy(t => t.Name))
                RelationshipTypes.Add(t);
            RelationshipType ??= RelationshipTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"RelationshipTypes load failed: {ex.Message}";
        }
    }

    public async Task ReloadForUniverseAsync()
    {
        UniverseEntities.Clear();
        Items.Clear();
        var u = _universes.SelectedUniverse;
        if (u is null) { StatusMessage = "Select a universe first."; return; }

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
                    Items.Add(new RelationshipRow(
                        r.Id,
                        r.SourceEntity?.Name ?? entities.FirstOrDefault(x => x.Id == r.SourceEntityId)?.Name ?? "?",
                        r.RelationshipType?.Name ?? "?",
                        r.TargetEntity?.Name ?? entities.FirstOrDefault(x => x.Id == r.TargetEntityId)?.Name ?? "?",
                        r.Description));
                }
            }
            StatusMessage = $"{Items.Count} relationship(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (SourceEntity is null || TargetEntity is null || RelationshipType is null)
        {
            StatusMessage = "Source, target, and relationship type are required.";
            return;
        }
        if (SourceEntity.Id == TargetEntity.Id)
        {
            StatusMessage = "Source and target must differ.";
            return;
        }
        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(_provider, async s =>
            {
                var rel = await s.CreateAsync(SourceEntity.Id, TargetEntity.Id, RelationshipType.Id);
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    rel.Description = Description.Trim();
                    await s.UpdateAsync(rel);
                }
            });
            Description = string.Empty;
            await ReloadForUniverseAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedRelationship is null) return;
        var id = SelectedRelationship.Id;
        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(_provider, s => s.DeleteAsync(id));
            await ReloadForUniverseAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }
}

public record RelationshipRow(Guid Id, string Source, string Type, string Target, string Description)
{
    public string Display => $"{Source}  ──{Type}──>  {Target}";
}
