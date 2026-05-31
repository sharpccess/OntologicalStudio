using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class UniverseCanvasViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;
    private readonly ILocalizationService _localization;
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OntologicalStudio",
        "startup.log");
    private CancellationTokenSource? _selectedNodeAutosaveCts;
    private CancellationTokenSource? _selectedRelationshipAutosaveCts;
    private bool _suspendSelectedNodeAutosave;
    private bool _suspendSelectedRelationshipAutosave;

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
    private string selectedEntityTypeText = string.Empty;

    [ObservableProperty]
    private RelationshipType? selectedRelationshipType;

    [ObservableProperty]
    private string selectedRelationshipTypeText = string.Empty;

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

    private bool isLinkMode;

    [ObservableProperty]
    private string selectedNodeName = string.Empty;

    [ObservableProperty]
    private string selectedNodeDescription = string.Empty;

    [ObservableProperty]
    private string selectedNodeNotes = string.Empty;

    [ObservableProperty]
    private EntityType? selectedNodeEntityType;

    [ObservableProperty]
    private string selectedNodeEntityTypeText = string.Empty;

    [ObservableProperty]
    private RelationshipType? selectedNodeRelationshipType;

    [ObservableProperty]
    private string selectedNodeRelationshipTypeText = string.Empty;

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

    public bool HasSelectedUniverse => _universes.SelectedUniverse is not null;
    public IServiceProvider ServiceProvider => _provider;
    public bool IsLinkMode
    {
        get => isLinkMode;
        set => SetProperty(ref isLinkMode, value);
    }

    partial void OnSelectedNodeNameChanged(string value) { }
    partial void OnSelectedNodeDescriptionChanged(string value) { }
    partial void OnSelectedNodeNotesChanged(string value) { }
    partial void OnSelectedNodeEntityTypeChanged(EntityType? value) { }
    partial void OnSelectedNodeRelationshipTypeChanged(RelationshipType? value) { }
    partial void OnSelectedNodeRelationshipDescriptionChanged(string value) { }

    public UniverseCanvasViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _localization = provider.GetRequiredService<ILocalizationService>();
        Hydration = new EntityHydrationViewModel(provider);
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
        _localization.OnLanguageChanged += HandleLanguageChanged;
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
                EntityTypes.Add(TypeLocalizationHelper.Localize(entityType, _localization));
            SelectedEntityType ??= EntityTypes.FirstOrDefault();
            SelectedEntityTypeText = SelectedEntityType?.DisplayName ?? string.Empty;

            var relationshipTypes = await ScopedRunner.RunAsync<IRelationshipTypeRepository, IEnumerable<RelationshipType>>(
                _provider,
                repository => repository.GetAllAsync());
            RelationshipTypes.Clear();
            foreach (var relationshipType in relationshipTypes.OrderBy(x => x.Name))
                RelationshipTypes.Add(TypeLocalizationHelper.Localize(relationshipType, _localization));
            SelectedRelationshipType ??= RelationshipTypes.FirstOrDefault();
            SelectedRelationshipTypeText = SelectedRelationshipType?.DisplayName ?? string.Empty;
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

        var universe = _universes.SelectedUniverse;
        if (universe is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Nodes.Clear();
                Edges.Clear();
                StatusMessage = "Select a universe to work on the graph canvas.";
            });
            return;
        }

        try
        {
            var entities = (await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider,
                service => service.GetByUniverseAsync(universe.Id))).ToList();

            var index = 0;
            var loadedNodes = new List<CanvasEntityNodeViewModel>();
            foreach (var entity in entities.OrderBy(x => x.Name))
            {
                if (entity.EntityType is not null)
                    TypeLocalizationHelper.Localize(entity.EntityType, _localization);

                var (width, height) = GetNodeSize(entity);
                var (x, y) = entity.PositionX == 0 && entity.PositionY == 0
                    ? GetDefaultPosition(index++)
                    : (entity.PositionX, entity.PositionY);

                loadedNodes.Add(new CanvasEntityNodeViewModel(entity, x, y, width, height));
            }

            var nodeById = loadedNodes.ToDictionary(x => x.Id);
            var relationList = new List<Relationship>();
            var seen = new HashSet<Guid>();

            foreach (var node in loadedNodes)
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
                    if (relation.RelationshipType is not null)
                        TypeLocalizationHelper.Localize(relation.RelationshipType, _localization);

                    relationList.Add(relation);
                }
            }

            var loadedEdges = new List<CanvasRelationshipEdgeViewModel>();
            foreach (var relation in relationList.OrderBy(x => x.RelationshipType?.Name).ThenBy(x => x.Id))
            {
                var source = nodeById[relation.SourceEntityId];
                var target = nodeById[relation.TargetEntityId];
                loadedEdges.Add(new CanvasRelationshipEdgeViewModel(
                    relation.Id,
                    source,
                    target,
                    relation.RelationshipTypeId,
                    relation.RelationshipType?.DisplayName ?? relation.RelationshipType?.Name ?? "relatesTo",
                    relation.Description));
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Nodes.Clear();
                Edges.Clear();

                foreach (var node in loadedNodes)
                    Nodes.Add(node);

                foreach (var edge in loadedEdges)
                    Edges.Add(edge);

                LinkSource = LinkSource is not null ? Nodes.FirstOrDefault(x => x.Id == LinkSource.Id) : Nodes.FirstOrDefault();
                LinkTarget = LinkTarget is not null ? Nodes.FirstOrDefault(x => x.Id == LinkTarget.Id) : Nodes.Skip(1).FirstOrDefault() ?? Nodes.FirstOrDefault();
                SelectedNode = SelectedNode is not null ? Nodes.FirstOrDefault(x => x.Id == SelectedNode.Id) : SelectedNode;

                var maxX = Nodes.Count == 0 ? 1600 : Math.Max(1600, Nodes.Max(x => x.X + x.Width + 120));
                var maxY = Nodes.Count == 0 ? 1000 : Math.Max(1000, Nodes.Max(x => x.Y + x.Height + 120));
                CanvasWidth = maxX;
                CanvasHeight = maxY;

                StatusMessage = $"{Nodes.Count} node(s) and {Edges.Count} relationship(s) in '{universe.Name}'. Double-click on the canvas to place a new entity.";
            });
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
            WriteStartupLog("UniverseCanvasViewModel CreateNodeAtAsync aborted: no selected universe.");
            return;
        }

        var entityType = entityTypeOverride is not null
            ? await ResolveEntityTypeAsync(entityTypeOverride.DisplayName, entityTypeOverride)
            : await ResolveEntityTypeAsync(SelectedEntityTypeText, SelectedEntityType ?? EntityTypes.FirstOrDefault());

        if (entityType is null)
        {
            StatusMessage = "Select an entity type first.";
            WriteStartupLog("UniverseCanvasViewModel CreateNodeAtAsync aborted: no entity type available.");
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewEntityName)
            ? "New Item"
            : NewEntityName.Trim();

        try
        {
            WriteStartupLog($"UniverseCanvasViewModel CreateNodeAtAsync start | universe={universe.Id} | type={entityType.Id} | name='{name}'");
            var entity = await ScopedRunner.RunAsync<IEntityService, Entity>(
                _provider,
                service => service.CreateAsync(name, NewEntityDescription.Trim(), entityType.Id, universe.Id));

            entity.PositionX = Math.Max(24, x);
            entity.PositionY = Math.Max(24, y);
            entity.EntityType = entityType;
            entity.UniverseId = universe.Id;
            entity.Properties = UpdateNodeLayoutProperties(entity.Properties, CanvasEntityNodeViewModel.DefaultWidth, CanvasEntityNodeViewModel.DefaultHeight);

            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.UpdateAsync(entity));

            var persistedEntity = await ScopedRunner.RunAsync<IEntityService, Entity>(
                _provider,
                service => service.GetByIdAsync(entity.Id));

            if (persistedEntity is null)
            {
                StatusMessage = "Create node failed: entity was not persisted.";
                WriteStartupLog($"UniverseCanvasViewModel CreateNodeAtAsync validation failed | entity={entity.Id}");
                await LoadAsync();
                return;
            }

            persistedEntity.EntityType = EntityTypes.FirstOrDefault(x => x.Id == persistedEntity.EntityTypeId) ?? entityType;

            var node = new CanvasEntityNodeViewModel(
                persistedEntity,
                persistedEntity.PositionX,
                persistedEntity.PositionY,
                CanvasEntityNodeViewModel.DefaultWidth,
                CanvasEntityNodeViewModel.DefaultHeight);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                NewEntityName = string.Empty;
                NewEntityDescription = string.Empty;
                Nodes.Add(node);
                SelectedNode = node;
                if (SelectedNode is not null)
                    ApplySelectedNodeSnapshot(SelectedNode);

                CanvasWidth = Math.Max(CanvasWidth, node.X + node.Width + 120);
                CanvasHeight = Math.Max(CanvasHeight, node.Y + node.Height + 120);
            });
            StatusMessage = "Item created. Edit it directly on the canvas or in the side panel.";
            WriteStartupLog($"UniverseCanvasViewModel CreateNodeAtAsync success | entity={entity.Id}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create node failed: {ex.Message}";
            WriteStartupLog($"UniverseCanvasViewModel CreateNodeAtAsync error: {ex}");
        }
    }

    public async Task PersistNodeLayoutAsync(CanvasEntityNodeViewModel node)
    {
        try
        {
            node.Entity.PositionX = node.X;
            node.Entity.PositionY = node.Y;
            node.Entity.Properties = UpdateNodeLayoutProperties(node.Entity.Properties, node.Width, node.Height);
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.UpdateAsync(node.Entity));

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
            ApplySelectedNodeSnapshot(null);
            return;
        }

        ApplySelectedNodeSnapshot(node);

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
            ApplySelectedRelationshipSnapshot(null);
            return;
        }

        SelectedNode = null;
        ApplySelectedRelationshipSnapshot(edge);
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
            node.Entity.PositionX = Math.Max(24, x);
            node.Entity.PositionY = Math.Max(24, y);
            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.UpdateAsync(node.Entity));

            await LoadAsync();
            _universes.NotifyDataChanged();
            SelectedNode = Nodes.FirstOrDefault(n => n.Id == node.Id);
            if (SelectedNode is not null)
            {
                ApplySelectedNodeSnapshot(SelectedNode);
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

        await SaveNodeAsync(
            SelectedNode,
            SelectedNodeName,
            SelectedNodeDescription,
            SelectedNodeNotes,
            SelectedNodeEntityType);
    }

    public async Task SaveNodeAsync(
        CanvasEntityNodeViewModel targetNode,
        string? nodeName,
        string? nodeDescription,
        string? nodeNotes,
        EntityType? nodeEntityType)
    {
        var selectedNode = targetNode;
        var nodeId = selectedNode.Id;
        var normalizedName = string.IsNullOrWhiteSpace(nodeName) ? "New Item" : nodeName.Trim();
        var normalizedDescription = nodeDescription?.Trim() ?? string.Empty;
        var normalizedNotes = nodeNotes?.Trim() ?? string.Empty;

        try
        {
            selectedNode.Entity.Name = normalizedName;
            selectedNode.Entity.Description = normalizedDescription;
            selectedNode.Entity.Notes = normalizedNotes;
            var resolvedEntityType = await ResolveEntityTypeAsync(SelectedNodeEntityTypeText, nodeEntityType);
            if (resolvedEntityType is not null)
            {
                selectedNode.Entity.EntityTypeId = resolvedEntityType.Id;
                selectedNode.Entity.EntityType = resolvedEntityType;
            }

            await ScopedRunner.RunAsync<IEntityService>(
                _provider,
                service => service.UpdateAsync(selectedNode.Entity));

            var persistedEntity = await ScopedRunner.RunAsync<IEntityService, Entity>(
                _provider,
                service => service.GetByIdAsync(nodeId));

            if (persistedEntity is null)
            {
                StatusMessage = "Save node skipped: entity no longer exists.";
                WriteStartupLog($"UniverseCanvasViewModel SaveNodeAsync validation failed | entity={nodeId}");
                await LoadAsync();
                return;
            }

            selectedNode.Entity.Name = persistedEntity.Name;
            selectedNode.Entity.Description = persistedEntity.Description;
            selectedNode.Entity.Notes = persistedEntity.Notes;
            selectedNode.Entity.EntityTypeId = persistedEntity.EntityTypeId;
            selectedNode.Entity.EntityType = EntityTypes.FirstOrDefault(x => x.Id == persistedEntity.EntityTypeId)
                ?? persistedEntity.EntityType
                ?? selectedNode.Entity.EntityType;

            selectedNode.RefreshDisplay();
            if (SelectedNode?.Id == selectedNode.Id)
                ApplySelectedNodeSnapshot(selectedNode);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save node failed: {ex.Message}";
            WriteStartupLog($"UniverseCanvasViewModel SaveSelectedNodeAsync error: {ex}");
        }
    }

    [RelayCommand]
    private async Task SaveSelectedRelationshipAsync()
    {
        if (SelectedEdge is null)
            return;

        var edgeId = SelectedEdge.Id;
        var previousLabel = SelectedEdge.Label;
        var relationshipType = await ResolveRelationshipTypeAsync(SelectedNodeRelationshipTypeText, SelectedNodeRelationshipType);
        if (relationshipType is null)
        {
            StatusMessage = "Relationship type is required.";
            return;
        }
        var relationshipDescription = SelectedNodeRelationshipDescription.Trim();

        try
        {
            await ScopedRunner.RunAsync<IRelationshipService>(
                _provider,
                async service =>
                {
                    var relationship = await service.GetByIdAsync(edgeId);
                    relationship.RelationshipTypeId = relationshipType.Id;
                    relationship.Description = relationshipDescription;
                    await service.UpdateAsync(relationship);
                });

            if (SelectedEdge is not null)
            {
                SelectedEdge.RelationshipTypeId = relationshipType.Id;
                SelectedEdge.Label = string.IsNullOrWhiteSpace(SelectedNodeRelationshipTypeText)
                    ? relationshipType.DisplayName
                    : SelectedNodeRelationshipTypeText.Trim();
                SelectedEdge.Description = relationshipDescription;
                ApplySelectedRelationshipSnapshot(SelectedEdge);
            }

            StatusMessage = previousLabel == SelectedEdge?.Label
                ? $"Relationship saved as '{SelectedEdge?.Label}'."
                : $"Relationship changed to '{SelectedEdge?.Label}'.";
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
            SelectedEdge = Edges.FirstOrDefault(x => x.Id == edge.Id);
            SelectedNodeRelationshipType = relationshipType;
            SelectedNodeRelationshipTypeText = relationshipType.DisplayName;
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
            SelectedEdge = null;
            ApplySelectedRelationshipSnapshot(null);
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
        if (LinkSource is null || LinkTarget is null)
        {
            StatusMessage = "Source, target and relationship type are required.";
            return;
        }

        var relationshipType = await ResolveRelationshipTypeAsync(SelectedRelationshipTypeText, SelectedRelationshipType);
        if (relationshipType is null)
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
                service => service.CreateAsync(LinkSource.Id, LinkTarget.Id, relationshipType.Id));
            IsLinkMode = false;
            LinkSource = null;
            LinkTarget = null;
            HasLinkPreview = false;
            await LoadAsync();
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete node failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private void ApplySelectedNodeSnapshot(CanvasEntityNodeViewModel? node)
    {
        _suspendSelectedNodeAutosave = true;
        try
        {
            if (node is null)
            {
                SelectedNodeName = string.Empty;
                SelectedNodeDescription = string.Empty;
                SelectedNodeNotes = string.Empty;
                SelectedNodeEntityType = null;
                return;
            }

        SelectedNodeName = node.Name;
        SelectedNodeDescription = node.Description;
        SelectedNodeNotes = node.Entity.Notes ?? string.Empty;
        SelectedNodeEntityType = EntityTypes.FirstOrDefault(x => x.Id == node.Entity.EntityTypeId);
        SelectedNodeEntityTypeText = SelectedNodeEntityType?.DisplayName ?? node.Entity.EntityType?.DisplayName ?? node.Entity.EntityType?.Name ?? string.Empty;
        }
        finally
        {
            _suspendSelectedNodeAutosave = false;
        }
    }

    private void QueueSelectedNodeAutosave()
    {
        if (_suspendSelectedNodeAutosave || SelectedNode is null)
            return;

        _selectedNodeAutosaveCts?.Cancel();
        _selectedNodeAutosaveCts?.Dispose();
        _selectedNodeAutosaveCts = new CancellationTokenSource();
        var cancellationToken = _selectedNodeAutosaveCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                await SaveSelectedNodeAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }, cancellationToken);
    }

    private void ApplySelectedRelationshipSnapshot(CanvasRelationshipEdgeViewModel? edge)
    {
        _suspendSelectedRelationshipAutosave = true;
        try
        {
            if (edge is null)
            {
                SelectedNodeRelationshipType = null;
                SelectedNodeRelationshipTypeText = string.Empty;
                SelectedNodeRelationshipDescription = string.Empty;
                return;
            }

            SelectedNodeRelationshipType = RelationshipTypes.FirstOrDefault(x => x.Id == edge.RelationshipTypeId);
            SelectedNodeRelationshipTypeText = edge.Label;
            SelectedNodeRelationshipDescription = edge.Description;
        }
        finally
        {
            _suspendSelectedRelationshipAutosave = false;
        }
    }

    private void QueueSelectedRelationshipAutosave()
    {
        if (_suspendSelectedRelationshipAutosave || SelectedEdge is null || SelectedNodeRelationshipType is null)
            return;

        _selectedRelationshipAutosaveCts?.Cancel();
        _selectedRelationshipAutosaveCts?.Dispose();
        _selectedRelationshipAutosaveCts = new CancellationTokenSource();
        var cancellationToken = _selectedRelationshipAutosaveCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                await SaveSelectedRelationshipAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }, cancellationToken);
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(StartupLogPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private async Task<RelationshipType?> ResolveRelationshipTypeAsync(string? input, RelationshipType? selectedType)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return selectedType;

        if (selectedType is not null && TypeLocalizationHelper.MatchesRelationshipTypeInput(selectedType, trimmed, _localization))
            return selectedType;

        var existing = RelationshipTypes.FirstOrDefault(x => TypeLocalizationHelper.MatchesRelationshipTypeInput(x, trimmed, _localization));
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

        TypeLocalizationHelper.Localize(created, _localization);
        RelationshipTypes.Add(created);
        SelectedRelationshipType = created;
        SelectedRelationshipTypeText = created.DisplayName;
        SelectedNodeRelationshipType = created;
        SelectedNodeRelationshipTypeText = created.DisplayName;
        return created;
    }

    private async Task<EntityType?> ResolveEntityTypeAsync(string? input, EntityType? selectedType)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return selectedType;

        if (selectedType is not null && TypeLocalizationHelper.MatchesEntityTypeInput(selectedType, trimmed, _localization))
            return selectedType;

        var existing = EntityTypes.FirstOrDefault(x => TypeLocalizationHelper.MatchesEntityTypeInput(x, trimmed, _localization));
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

        TypeLocalizationHelper.Localize(created, _localization);
        EntityTypes.Add(created);
        SelectedEntityType = created;
        SelectedEntityTypeText = created.DisplayName;
        SelectedNodeEntityType = created;
        SelectedNodeEntityTypeText = created.DisplayName;
        return created;
    }

    private void HandleLanguageChanged()
    {
        _ = LoadReferenceDataAsync();
        _ = LoadAsync();
    }

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
    public string TypeName => Entity.EntityType?.DisplayName ?? Entity.EntityType?.Name ?? "Entity";
    public string Description => Entity.Description;

    private double x;
    private double y;
    private double width;
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

    public double X
    {
        get => x;
        set
        {
            if (SetProperty(ref x, value))
                PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double Y
    {
        get => y;
        set
        {
            if (SetProperty(ref y, value))
                PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double Width
    {
        get => width;
        set
        {
            if (SetProperty(ref width, value))
                PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double Height
    {
        get => height;
        set
        {
            if (SetProperty(ref height, value))
                PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(TypeName));
        OnPropertyChanged(nameof(Description));
    }

}

public class CanvasRelationshipEdgeViewModel
{
    public Guid Id { get; }
    public Guid RelationshipTypeId { get; set; }
    public CanvasEntityNodeViewModel Source { get; }
    public CanvasEntityNodeViewModel Target { get; }
    public string Label { get; set; }
    public string Description { get; set; }

    public CanvasRelationshipEdgeViewModel(
        Guid id,
        CanvasEntityNodeViewModel source,
        CanvasEntityNodeViewModel target,
        Guid relationshipTypeId,
        string label,
        string description)
    {
        Id = id;
        RelationshipTypeId = relationshipTypeId;
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