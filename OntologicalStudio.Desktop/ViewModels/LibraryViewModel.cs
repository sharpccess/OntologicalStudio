using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;
    private readonly EntitiesViewModel _entities;
    private readonly UniverseCanvasViewModel _canvas;
    private readonly List<EntityLibraryItem> _allEntityItems = new();
    private readonly List<UniverseModelLibraryItem> _allUniverseModelItems = new();

    public ObservableCollection<EntityLibraryItem> EntityItems { get; } = new();
    public ObservableCollection<UniverseModelLibraryItem> UniverseModelItems { get; } = new();

    [ObservableProperty]
    private string entitySearchText = string.Empty;

    [ObservableProperty]
    private string universeModelSearchText = string.Empty;

    [ObservableProperty]
    private EntityLibraryItem? selectedEntityItem;

    [ObservableProperty]
    private UniverseModelLibraryItem? selectedUniverseModelItem;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LibraryViewModel(IServiceProvider provider, UniversesViewModel universes, EntitiesViewModel entities, UniverseCanvasViewModel canvas)
    {
        _provider = provider;
        _universes = universes;
        _entities = entities;
        _canvas = canvas;
    }

    public async Task LoadAsync()
    {
        _allEntityItems.Clear();
        _allUniverseModelItems.Clear();

        var entities = await ScopedRunner.RunAsync<ILibraryCatalogService, IReadOnlyList<EntityLibraryItem>>(
            _provider,
            service => service.GetEntityItemsAsync());
        foreach (var item in entities)
            _allEntityItems.Add(item);

        var models = await ScopedRunner.RunAsync<ILibraryCatalogService, IReadOnlyList<UniverseModelLibraryItem>>(
            _provider,
            service => service.GetUniverseModelItemsAsync());
        foreach (var item in models)
            _allUniverseModelItems.Add(item);

        ApplyFilters();
        StatusMessage = $"Library loaded: {EntityItems.Count} entities, {UniverseModelItems.Count} models.";
    }

    [RelayCommand]
    private async Task SaveSelectedEntityAsync()
    {
        var entity = _canvas.SelectedNode?.Entity ?? _entities.SelectedEntity;
        if (entity is null)
        {
            StatusMessage = "Select an entity first.";
            return;
        }

        await ScopedRunner.RunAsync<ILibraryCatalogService, EntityLibraryItem>(
            _provider,
            service => service.SaveEntityAsync(new EntityLibraryItem
            {
                Name = entity.Name,
                Description = entity.Description,
                EntityTypeName = entity.EntityType?.Name ?? string.Empty,
                Notes = entity.Notes,
                HydrationData = entity.HydrationData,
                ConfidenceLevel = entity.ConfidenceLevel,
                CompletenessScore = entity.CompletenessScore,
                Properties = entity.Properties
            }));

        await LoadAsync();
        StatusMessage = $"Entity '{entity.Name}' saved to library.";
    }

    [RelayCommand]
    private async Task ImportSelectedEntityAsync()
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null || SelectedEntityItem is null)
        {
            StatusMessage = "Select an active universe and a library entity.";
            return;
        }

        var entityType = await ResolveEntityTypeAsync(SelectedEntityItem.EntityTypeName);
        if (entityType is null)
        {
            StatusMessage = "Entity type could not be resolved.";
            return;
        }

        var created = await ScopedRunner.RunAsync<IEntityService, Entity>(
            _provider,
            service => service.CreateAsync(
                SelectedEntityItem.Name,
                SelectedEntityItem.Description,
                entityType.Id,
                universe.Id));

        created.Notes = SelectedEntityItem.Notes;
        created.HydrationData = SelectedEntityItem.HydrationData;
        created.ConfidenceLevel = SelectedEntityItem.ConfidenceLevel;
        created.CompletenessScore = SelectedEntityItem.CompletenessScore;
        created.Properties = SelectedEntityItem.Properties;

        await ScopedRunner.RunAsync<IEntityService>(
            _provider,
            service => service.UpdateAsync(created));

        await _entities.LoadAsync();
        await _canvas.LoadAsync();
        _universes.NotifyDataChanged();
        StatusMessage = $"Entity '{SelectedEntityItem.Name}' imported into '{universe.Name}'.";
    }

    [RelayCommand]
    private async Task DeleteSelectedEntityAsync()
    {
        if (SelectedEntityItem is null)
        {
            StatusMessage = "Select a library entity first.";
            return;
        }

        var name = SelectedEntityItem.Name;
        await ScopedRunner.RunAsync<ILibraryCatalogService>(
            _provider,
            service => service.DeleteEntityAsync(SelectedEntityItem.Id));

        SelectedEntityItem = null;
        await LoadAsync();
        StatusMessage = $"Entity '{name}' removed from library.";
    }

    [RelayCommand]
    private async Task SaveActiveUniverseModelAsync()
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null)
        {
            StatusMessage = "Select an active universe first.";
            return;
        }

        var entities = (await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
            _provider,
            service => service.GetByUniverseAsync(universe.Id))).ToList();

        var relationships = new List<Relationship>();
        foreach (var entity in entities)
        {
            var rels = await ScopedRunner.RunAsync<IRelationshipService, IEnumerable<Relationship>>(
                _provider,
                service => service.GetBySourceEntityAsync(entity.Id));
            relationships.AddRange(rels.Where(x => relationships.All(existing => existing.Id != x.Id)));
        }

        var model = new UniverseModelLibraryItem
        {
            Name = universe.Name,
            Description = universe.Description,
            Entities = entities.Select(entity => new LibraryEntitySnapshot
            {
                SnapshotId = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                EntityTypeName = entity.EntityType?.Name ?? string.Empty,
                Notes = entity.Notes,
                HydrationData = entity.HydrationData,
                ConfidenceLevel = entity.ConfidenceLevel,
                CompletenessScore = entity.CompletenessScore,
                Properties = entity.Properties,
                PositionX = entity.PositionX,
                PositionY = entity.PositionY
            }).ToList(),
            Relationships = relationships.Select(relationship => new LibraryRelationshipSnapshot
            {
                SourceSnapshotId = relationship.SourceEntityId,
                TargetSnapshotId = relationship.TargetEntityId,
                RelationshipTypeName = relationship.RelationshipType?.Name ?? string.Empty,
                Description = relationship.Description,
                Properties = relationship.Properties
            }).ToList()
        };

        await ScopedRunner.RunAsync<ILibraryCatalogService, UniverseModelLibraryItem>(
            _provider,
            service => service.SaveUniverseModelAsync(model));

        await LoadAsync();
        StatusMessage = $"Universe model '{universe.Name}' saved to library.";
    }

    [RelayCommand]
    private async Task ImportSelectedUniverseModelAsync()
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null || SelectedUniverseModelItem is null)
        {
            StatusMessage = "Select an active universe and a library model.";
            return;
        }

        var entityMap = new Dictionary<Guid, Guid>();

        foreach (var entitySnapshot in SelectedUniverseModelItem.Entities)
        {
            var entityType = await ResolveEntityTypeAsync(entitySnapshot.EntityTypeName);
            if (entityType is null)
                continue;

            var created = await ScopedRunner.RunAsync<IEntityService, Entity>(
                _provider,
                service => service.CreateAsync(
                    entitySnapshot.Name,
                    entitySnapshot.Description,
                    entityType.Id,
                    universe.Id));

            created.Notes = entitySnapshot.Notes;
            created.HydrationData = entitySnapshot.HydrationData;
            created.ConfidenceLevel = entitySnapshot.ConfidenceLevel;
            created.CompletenessScore = entitySnapshot.CompletenessScore;
            created.Properties = entitySnapshot.Properties;
            created.PositionX = entitySnapshot.PositionX;
            created.PositionY = entitySnapshot.PositionY;

            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.UpdateAsync(created));

            entityMap[entitySnapshot.SnapshotId] = created.Id;
        }

        foreach (var relationshipSnapshot in SelectedUniverseModelItem.Relationships)
        {
            if (!entityMap.TryGetValue(relationshipSnapshot.SourceSnapshotId, out var sourceId) ||
                !entityMap.TryGetValue(relationshipSnapshot.TargetSnapshotId, out var targetId))
                continue;

            var relationshipType = await ResolveRelationshipTypeAsync(relationshipSnapshot.RelationshipTypeName);
            if (relationshipType is null)
                continue;

            var createdRelationship = await ScopedRunner.RunAsync<IRelationshipService, Relationship>(
                _provider,
                service => service.CreateAsync(sourceId, targetId, relationshipType.Id));

            createdRelationship.Description = relationshipSnapshot.Description;
            createdRelationship.Properties = relationshipSnapshot.Properties;

            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                service => service.UpdateAsync(createdRelationship));
        }

        await _entities.LoadAsync();
        await _canvas.LoadAsync();
        _universes.NotifyDataChanged();
        StatusMessage = $"Universe model '{SelectedUniverseModelItem.Name}' imported into '{universe.Name}'.";
    }

    [RelayCommand]
    private async Task DeleteSelectedUniverseModelAsync()
    {
        if (SelectedUniverseModelItem is null)
        {
            StatusMessage = "Select a library model first.";
            return;
        }

        var name = SelectedUniverseModelItem.Name;
        await ScopedRunner.RunAsync<ILibraryCatalogService>(
            _provider,
            service => service.DeleteUniverseModelAsync(SelectedUniverseModelItem.Id));

        SelectedUniverseModelItem = null;
        await LoadAsync();
        StatusMessage = $"Universe model '{name}' removed from library.";
    }

    private async Task<EntityType?> ResolveEntityTypeAsync(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        var types = await ScopedRunner.RunAsync<IEntityTypeRepository, IEnumerable<EntityType>>(
            _provider,
            repository => repository.GetAllAsync());
        var existing = types.FirstOrDefault(x => string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return existing;

        var created = new EntityType
        {
            Name = trimmed,
            Description = trimmed
        };

        await ScopedRunner.RunAsync<IEntityTypeRepository>(
            _provider,
            repository => repository.AddAsync(created));

        return created;
    }

    private async Task<RelationshipType?> ResolveRelationshipTypeAsync(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        var types = await ScopedRunner.RunAsync<IRelationshipTypeRepository, IEnumerable<RelationshipType>>(
            _provider,
            repository => repository.GetAllAsync());
        var existing = types.FirstOrDefault(x => string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return existing;

        var created = new RelationshipType
        {
            Name = trimmed,
            Description = trimmed
        };

        await ScopedRunner.RunAsync<IRelationshipTypeRepository>(
            _provider,
            repository => repository.AddAsync(created));

        return created;
    }

    partial void OnEntitySearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnUniverseModelSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var entityQuery = EntitySearchText?.Trim() ?? string.Empty;
        var modelQuery = UniverseModelSearchText?.Trim() ?? string.Empty;

        var filteredEntities = string.IsNullOrWhiteSpace(entityQuery)
            ? _allEntityItems
            : _allEntityItems
                .Where(item =>
                    ContainsFilter(item.Name, entityQuery) ||
                    ContainsFilter(item.EntityTypeName, entityQuery) ||
                    ContainsFilter(item.Description, entityQuery) ||
                    ContainsFilter(item.Notes, entityQuery))
                .ToList();

        var filteredModels = string.IsNullOrWhiteSpace(modelQuery)
            ? _allUniverseModelItems
            : _allUniverseModelItems
                .Where(item =>
                    ContainsFilter(item.Name, modelQuery) ||
                    ContainsFilter(item.Description, modelQuery) ||
                    item.Entities.Any(entity =>
                        ContainsFilter(entity.Name, modelQuery) ||
                        ContainsFilter(entity.EntityTypeName, modelQuery)) ||
                    item.Relationships.Any(relationship =>
                        ContainsFilter(relationship.RelationshipTypeName, modelQuery)))
                .ToList();

        ReplaceItems(EntityItems, filteredEntities);
        ReplaceItems(UniverseModelItems, filteredModels);
    }

    private static bool ContainsFilter(string? text, string filter)
    {
        return !string.IsNullOrWhiteSpace(text)
            && text.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }
}