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
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class UniverseCanvasViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;

    public ObservableCollection<CanvasEntityNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<CanvasRelationshipEdgeViewModel> Edges { get; } = new();
    public ObservableCollection<EntityType> EntityTypes { get; } = new();
    public ObservableCollection<RelationshipType> RelationshipTypes { get; } = new();
    public EntityHydrationViewModel Hydration { get; }

    [ObservableProperty]
    private CanvasEntityNodeViewModel? selectedNode;

    [ObservableProperty]
    private EntityType? selectedEntityType;

    [ObservableProperty]
    private RelationshipType? selectedRelationshipType;

    [ObservableProperty]
    private CanvasEntityNodeViewModel? linkSource;

    [ObservableProperty]
    private CanvasEntityNodeViewModel? linkTarget;

    [ObservableProperty]
    private string newEntityName = string.Empty;

    [ObservableProperty]
    private string newEntityDescription = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Select a universe to work on the graph canvas.";

    [ObservableProperty]
    private double canvasWidth = 1800;

    [ObservableProperty]
    private double canvasHeight = 1200;

    [ObservableProperty]
    private double zoomScale = 1.0;

    [ObservableProperty]
    private bool isLinkMode;

    [ObservableProperty]
    private string selectedNodeName = string.Empty;

    [ObservableProperty]
    private string selectedNodeDescription = string.Empty;

    [ObservableProperty]
    private EntityType? selectedNodeEntityType;

    [ObservableProperty]
    private RelationshipType? selectedNodeRelationshipType;

    [ObservableProperty]
    private string selectedNodeRelationshipDescription = string.Empty;

    [ObservableProperty]
    private CanvasRelationshipEdgeViewModel? selectedEdge;

    [ObservableProperty]
    private double linkPreviewX;

    [ObservableProperty]
    private double linkPreviewY;

    [ObservableProperty]
    private bool hasLinkPreview;

    public UniverseCanvasViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        Hydration = new EntityHydrationViewModel(provider);
        _universes.SelectionChanged += async () => await LoadAsync();
        _universes.UniversesChanged += async () => await LoadAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var entityTypes = await ScopedRunner.RunAsync<IEntityTypeRepository, IEnumerable<EntityType>>(
                _provider,
                repository => repository.GetAllAsync());
            EntityTypes.Clear();
            foreach (var entityType in entityTypes.OrderBy(x => x.Name))
                EntityTypes.Add(entityType);
            SelectedEntityType ??= EntityTypes.FirstOrDefault();

            var relationshipTypes = await ScopedRunner.RunAsync<IRelationshipTypeRepository, IEnumerable<RelationshipType>>(
                _provider,
                repository => repository.GetAllAsync());
            RelationshipTypes.Clear();
            foreach (var relationshipType in relationshipTypes.OrderBy(x => x.Name))
                RelationshipTypes.Add(relationshipType);
            SelectedRelationshipType ??= RelationshipTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reference data error: {ex.Message}";
        }
    }

    public async Task LoadAsync()
    {
        if (EntityTypes.Count == 0 || RelationshipTypes.Count == 0)
            await LoadReferenceDataAsync();

        Nodes.Clear();
        Edges.Clear();

        var universe = _universes.SelectedUniverse;
        if (universe is null)
        {
            StatusMessage = "Select a universe to work on the graph canvas.";
            return;
        }

        try
        {
            var entities = (await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider,
                service => service.GetByUniverseAsync(universe.Id))).ToList();

            var index = 0;
            foreach (var entity in entities.OrderBy(x => x.Name))
            {
                var (width, height) = GetNodeSize(entity);
                var (x, y) = entity.PositionX == 0 && entity.PositionY == 0
                    ? GetDefaultPosition(index++)
                    : (entity.PositionX, entity.PositionY);

                Nodes.Add(new CanvasEntityNodeViewModel(entity, x, y, width, height));
            }

            var nodeById = Nodes.ToDictionary(x => x.Id);
            var relationList = new List<Relationship>();
            var seen = new HashSet<Guid>();

            foreach (var node in Nodes)
            {
                var relations = await ScopedRunner.RunAsync<IRelationshipService, IEnumerable<Relationship>>(
                    _provider,
                    service => service.GetBySourceEntityAsync(node.Id));

                foreach (var relation in relations)
                {
                    if (!seen.Add(relation.Id))
                        continue;
                    if (!nodeById.ContainsKey(relation.TargetEntityId))
                        continue;

                    relationList.Add(relation);
                }
            }

            foreach (var relation in relationList.OrderBy(x => x.RelationshipType?.Name).ThenBy(x => x.Id))
            {
                var source = nodeById[relation.SourceEntityId];
                var target = nodeById[relation.TargetEntityId];
                Edges.Add(new CanvasRelationshipEdgeViewModel(
                    relation.Id,
                    source,
                    target,
                    relation.RelationshipType?.Name ?? "relatesTo",
                    relation.Description));
            }

            LinkSource = LinkSource is not null ? Nodes.FirstOrDefault(x => x.Id == LinkSource.Id) : Nodes.FirstOrDefault();
            LinkTarget = LinkTarget is not null ? Nodes.FirstOrDefault(x => x.Id == LinkTarget.Id) : Nodes.Skip(1).FirstOrDefault() ?? Nodes.FirstOrDefault();
            SelectedNode = SelectedNode is not null ? Nodes.FirstOrDefault(x => x.Id == SelectedNode.Id) : SelectedNode;

            var maxX = Nodes.Count == 0 ? 1600 : Math.Max(1600, Nodes.Max(x => x.X + x.Width + 120));
            var maxY = Nodes.Count == 0 ? 1000 : Math.Max(1000, Nodes.Max(x => x.Y + x.Height + 120));
            CanvasWidth = maxX;
            CanvasHeight = maxY;

            StatusMessage = $"{Nodes.Count} node(s) and {Edges.Count} relationship(s) in '{universe.Name}'. Double-click on the canvas to place a new entity.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Canvas load failed: {ex.Message}";
        }
    }

    public async Task CreateNodeAtAsync(double x, double y, EntityType? entityTypeOverride = null)
    {
        var universe = _universes.SelectedUniverse;
        if (universe is null)
        {
            StatusMessage = "Select a universe first.";
            return;
        }

        var entityType = entityTypeOverride ?? SelectedEntityType;

        if (entityType is null)
        {
            StatusMessage = "Select an entity type first.";
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewEntityName)
            ? "New Item"
            : NewEntityName.Trim();

        try
        {
            var entity = await ScopedRunner.RunAsync<IEntityService, Entity>(
                _provider,
                service => service.CreateAsync(name, NewEntityDescription.Trim(), entityType.Id, universe.Id));

            entity.PositionX = Math.Max(24, x);
            entity.PositionY = Math.Max(24, y);

            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                async service =>
                {
                    var persisted = await service.GetByIdAsync(entity.Id);
                    persisted.PositionX = entity.PositionX;
                    persisted.PositionY = entity.PositionY;
                    persisted.Properties = UpdateNodeLayoutProperties(persisted.Properties, CanvasEntityNodeViewModel.DefaultWidth, CanvasEntityNodeViewModel.DefaultHeight);
                    await service.UpdateAsync(persisted);
                });

            NewEntityName = string.Empty;
            NewEntityDescription = string.Empty;
            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedNode = Nodes.FirstOrDefault(node => node.Id == entity.Id);
            if (SelectedNode is not null)
            {
                SelectedNodeName = SelectedNode.Name;
                SelectedNodeDescription = SelectedNode.Description;
                SelectedNodeEntityType = EntityTypes.FirstOrDefault(x => x.Id == SelectedNode.Entity.EntityTypeId);
            }
            StatusMessage = "Item created. Edit it directly on the canvas or in the side panel.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create node failed: {ex.Message}";
        }
    }

    public async Task PersistNodeLayoutAsync(CanvasEntityNodeViewModel node)
    {
        try
        {
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                async service =>
                {
                    var entity = await service.GetByIdAsync(node.Id);
                    entity.PositionX = node.X;
                    entity.PositionY = node.Y;
                    entity.Properties = UpdateNodeLayoutProperties(entity.Properties, node.Width, node.Height);
                    await service.UpdateAsync(entity);
                });

            var maxX = Math.Max(1600, Nodes.Max(x => x.X + x.Width + 120));
            var maxY = Math.Max(1000, Nodes.Max(x => x.Y + x.Height + 120));
            CanvasWidth = maxX;
            CanvasHeight = maxY;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Move node failed: {ex.Message}";
        }
    }

    public void SelectNode(CanvasEntityNodeViewModel? node)
    {
        SelectedNode = node;
        SelectedEdge = null;
        Hydration.SelectedNode = node;
        if (node is null)
        {
            SelectedNodeName = string.Empty;
            SelectedNodeDescription = string.Empty;
            SelectedNodeEntityType = null;
            return;
        }

        SelectedNodeName = node.Name;
        SelectedNodeDescription = node.Description;
        SelectedNodeEntityType = EntityTypes.FirstOrDefault(x => x.Id == node.Entity.EntityTypeId);

        if (IsLinkMode && LinkSource is null)
        {
            LinkSource = node;
        }
        else if (IsLinkMode && LinkSource is not null && LinkSource.Id != node.Id)
        {
            LinkTarget = node;
        }
        else
        {
            LinkSource ??= node;
            LinkTarget ??= Nodes.FirstOrDefault(x => x.Id != node.Id) ?? node;
        }
    }

    public void SelectEdge(CanvasRelationshipEdgeViewModel? edge)
    {
        SelectedEdge = edge;
        if (edge is null)
        {
            SelectedNodeRelationshipType = null;
            SelectedNodeRelationshipDescription = string.Empty;
            return;
        }

        SelectedNode = null;
        SelectedNodeRelationshipType = RelationshipTypes.FirstOrDefault(x => x.Name == edge.Label);
        SelectedNodeRelationshipDescription = edge.Description;
    }

    public void StartConnection(CanvasEntityNodeViewModel node)
    {
        SelectedNode = node;
        IsLinkMode = true;
        LinkSource = node;
        LinkTarget = null;
        HasLinkPreview = false;
        StatusMessage = $"Connection mode: source is '{node.Name}'. Click another node to create the relationship.";
    }

    public void CancelConnection()
    {
        IsLinkMode = false;
        LinkSource = null;
        LinkTarget = null;
        HasLinkPreview = false;
        StatusMessage = "Connection mode cancelled.";
    }

    public void UpdateLinkPreview(double x, double y)
    {
        if (!IsLinkMode || LinkSource is null)
        {
            HasLinkPreview = false;
            return;
        }

        LinkPreviewX = x;
        LinkPreviewY = y;
        HasLinkPreview = true;
    }

    public async Task DeleteNodeAsync(CanvasEntityNodeViewModel node)
    {
        SelectedNode = node;
        await DeleteSelectedNodeAsync();
    }

    public async Task MoveExistingNodeAsync(CanvasEntityNodeViewModel node, double x, double y)
    {
        try
        {
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                async service =>
                {
                    var entity = await service.GetByIdAsync(node.Id);
                    entity.PositionX = Math.Max(24, x);
                    entity.PositionY = Math.Max(24, y);
                    await service.UpdateAsync(entity);
                });

            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedNode = Nodes.FirstOrDefault(n => n.Id == node.Id);
            if (SelectedNode is not null)
            {
                SelectedNodeName = SelectedNode.Name;
                SelectedNodeDescription = SelectedNode.Description;
            }
            StatusMessage = $"Moved '{node.Name}' to the selected canvas position.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Move existing node failed: {ex.Message}";
        }
    }

    public async Task SaveSelectedNodeAsync()
    {
        if (SelectedNode is null)
            return;

        try
        {
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                async service =>
                {
                    var entity = await service.GetByIdAsync(SelectedNode.Id);
                    entity.Name = string.IsNullOrWhiteSpace(SelectedNodeName) ? "New Item" : SelectedNodeName.Trim();
                    entity.Description = SelectedNodeDescription.Trim();
                    if (SelectedNodeEntityType is not null)
                        entity.EntityTypeId = SelectedNodeEntityType.Id;
                    await service.UpdateAsync(entity);
                });

            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedNode = Nodes.FirstOrDefault(x => x.Id == SelectedNode?.Id);
            if (SelectedNode is not null)
            {
                SelectedNodeName = SelectedNode.Name;
                SelectedNodeDescription = SelectedNode.Description;
                SelectedNodeEntityType = EntityTypes.FirstOrDefault(x => x.Id == SelectedNode.Entity.EntityTypeId);
            }
            StatusMessage = "Node updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save node failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveSelectedRelationshipAsync()
    {
        if (SelectedEdge is null || SelectedNodeRelationshipType is null)
            return;

        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                async service =>
                {
                    var relationship = await service.GetByIdAsync(SelectedEdge.Id);
                    relationship.RelationshipTypeId = SelectedNodeRelationshipType.Id;
                    relationship.Description = SelectedNodeRelationshipDescription.Trim();
                    await service.UpdateAsync(relationship);
                });

            await LoadAsync();
            _universes.NotifyDataChanged();
            StatusMessage = "Relationship updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save relationship failed: {ex.Message}";
        }
    }

    public async Task UpdateEdgeTypeAsync(CanvasRelationshipEdgeViewModel edge, RelationshipType relationshipType)
    {
        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                async service =>
                {
                    var relationship = await service.GetByIdAsync(edge.Id);
                    relationship.RelationshipTypeId = relationshipType.Id;
                    relationship.RelationshipType = null!;
                    await service.UpdateAsync(relationship);
                });

            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedEdge = Edges.FirstOrDefault(x => x.Id == edge.Id);
            SelectedNodeRelationshipType = relationshipType;
            StatusMessage = $"Relationship changed to '{relationshipType.Name}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update relationship type failed: {ex.Message}";
        }
    }

    public async Task DeleteEdgeAsync(CanvasRelationshipEdgeViewModel edge)
    {
        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                service => service.DeleteAsync(edge.Id));

            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedEdge = null;
            SelectedNodeRelationshipType = null;
            SelectedNodeRelationshipDescription = string.Empty;
            StatusMessage = "Relationship deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete relationship failed: {ex.Message}";
        }
    }

    public async Task HandleNodeClickedAsync(CanvasEntityNodeViewModel node)
    {
        SelectNode(node);
        if (!IsLinkMode)
            return;

        if (LinkSource is null)
        {
            LinkSource = node;
            StatusMessage = $"Link start: {node.Name}. Click another node to create the relationship.";
            return;
        }

        if (LinkSource.Id == node.Id)
            return;

        LinkTarget = node;
        await ConnectAsync();
    }

    [RelayCommand]
    private void ToggleLinkMode()
    {
        IsLinkMode = !IsLinkMode;
        LinkSource = null;
        LinkTarget = null;
        StatusMessage = IsLinkMode
            ? "Link mode enabled. Click a source node, then a target node."
            : "Link mode disabled.";
    }

    [RelayCommand]
    private void ZoomIn() => ZoomScale = Math.Min(2.5, ZoomScale + 0.1);

    [RelayCommand]
    private void ZoomOut() => ZoomScale = Math.Max(0.4, ZoomScale - 0.1);

    [RelayCommand]
    private void ResetZoom() => ZoomScale = 1.0;

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (LinkSource is null || LinkTarget is null || SelectedRelationshipType is null)
        {
            StatusMessage = "Source, target and relationship type are required.";
            return;
        }

        if (LinkSource.Id == LinkTarget.Id)
        {
            StatusMessage = "Source and target must be different.";
            return;
        }

        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                service => service.CreateAsync(LinkSource.Id, LinkTarget.Id, SelectedRelationshipType.Id));
            IsLinkMode = false;
            LinkSource = null;
            LinkTarget = null;
            HasLinkPreview = false;
            await LoadAsync();
            _universes.NotifyDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create relationship failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedNodeAsync()
    {
        if (SelectedNode is null)
            return;

        try
        {
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.DeleteAsync(SelectedNode.Id));
            await LoadAsync();
            _universes.NotifyDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete node failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private static (double x, double y) GetDefaultPosition(int index)
    {
        var column = index % 5;
        var row = index / 5;
        return (80 + (column * 240), 80 + (row * 160));
    }

    private static (double width, double height) GetNodeSize(Entity entity)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entity.Properties))
                return (CanvasEntityNodeViewModel.DefaultWidth, CanvasEntityNodeViewModel.DefaultHeight);

            var json = JsonNode.Parse(entity.Properties)?.AsObject();
            var width = json?["canvasWidth"]?.GetValue<double?>() ?? CanvasEntityNodeViewModel.DefaultWidth;
            var height = json?["canvasHeight"]?.GetValue<double?>() ?? CanvasEntityNodeViewModel.DefaultHeight;
            return (Math.Max(160, width), Math.Max(100, height));
        }
        catch
        {
            return (CanvasEntityNodeViewModel.DefaultWidth, CanvasEntityNodeViewModel.DefaultHeight);
        }
    }

    private static string UpdateNodeLayoutProperties(string? currentProperties, double width, double height)
    {
        JsonObject json;
        try
        {
            json = JsonNode.Parse(string.IsNullOrWhiteSpace(currentProperties) ? "{}" : currentProperties!)?.AsObject()
                ?? new JsonObject();
        }
        catch
        {
            json = new JsonObject();
        }

        json["canvasWidth"] = Math.Max(160, width);
        json["canvasHeight"] = Math.Max(100, height);
        return json.ToJsonString();
    }
}

public partial class CanvasEntityNodeViewModel : ObservableObject
{
    public const double DefaultWidth = 180;
    public const double DefaultHeight = 132;

    public Entity Entity { get; }
    public Guid Id => Entity.Id;
    public string Name => Entity.Name;
    public string TypeName => Entity.EntityType?.Name ?? "Entity";
    public string Description => Entity.Description;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double width;

    [ObservableProperty]
    private double height;

    public event EventHandler? PositionChanged;

    public CanvasEntityNodeViewModel(Entity entity, double x, double y, double width, double height)
    {
        Entity = entity;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    partial void OnXChanged(double value) => PositionChanged?.Invoke(this, EventArgs.Empty);
    partial void OnYChanged(double value) => PositionChanged?.Invoke(this, EventArgs.Empty);
    partial void OnWidthChanged(double value) => PositionChanged?.Invoke(this, EventArgs.Empty);
    partial void OnHeightChanged(double value) => PositionChanged?.Invoke(this, EventArgs.Empty);
}

public class CanvasRelationshipEdgeViewModel
{
    public Guid Id { get; }
    public CanvasEntityNodeViewModel Source { get; }
    public CanvasEntityNodeViewModel Target { get; }
    public string Label { get; }
    public string Description { get; }

    public CanvasRelationshipEdgeViewModel(
        Guid id,
        CanvasEntityNodeViewModel source,
        CanvasEntityNodeViewModel target,
        string label,
        string description)
    {
        Id = id;
        Source = source;
        Target = target;
        Label = label;
        Description = description;
    }

    public double StartX => Source.X + (Source.Width / 2);
    public double StartY => Source.Y + (Source.Height / 2);
    public double EndX => Target.X + (Target.Width / 2);
    public double EndY => Target.Y + (Target.Height / 2);
    public double LabelX => ((StartX + EndX) / 2) - 50;
    public double LabelY => ((StartY + EndY) / 2) - 14;

    public string PathData
    {
        get
        {
            var delta = Math.Abs(EndX - StartX) / 2;
            var control = Math.Max(48, Math.Min(140, delta));
            return $"M {StartX},{StartY} C {StartX + control},{StartY} {EndX - control},{EndY} {EndX},{EndY}";
        }
    }
}