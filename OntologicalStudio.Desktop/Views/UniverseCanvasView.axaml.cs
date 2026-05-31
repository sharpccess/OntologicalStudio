using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Shapes;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Desktop.ViewModels;
using System.Collections.Specialized;
using System.Reactive.Linq;
using ShapePath = Avalonia.Controls.Shapes.Path;
using MenuItem = Avalonia.Controls.MenuItem;

namespace OntologicalStudio.Desktop.Views;

public partial class UniverseCanvasView : UserControl
{
    private Canvas? _canvas;
    private Border? _canvasContextSurface;
    private ScrollViewer? _scrollViewer;
    private UniverseCanvasViewModel? _viewModel;
    private CanvasEntityNodeViewModel? _dragNode;
    private CanvasEntityNodeViewModel? _resizeNode;
    private Point _dragStart;
    private double _originX;
    private double _originY;
    private double _originWidth;
    private double _originHeight;
    private bool _isPanning;
    private bool _isDraggingNode;
    private bool _isResizingNode;
    private bool _suppressContextMenu;
    private Point _panStart;
    private Vector _panOffset;
    private Point _lastCanvasPoint;

    public UniverseCanvasView()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.FindControl<Canvas>("CanvasHost");
        _canvasContextSurface = this.FindControl<Border>("CanvasContextSurface");
        _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollHost");
        DataContextChanged += OnDataContextChanged;
        if (_canvasContextSurface is not null)
        {
            _canvasContextSurface.PointerPressed += OnCanvasPointerPressed;
            _canvasContextSurface.ContextRequested += OnCanvasContextRequested;
        }
        if (_canvas is not null)
        {
            _canvas.DoubleTapped += OnCanvasDoubleTapped;
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
            _canvas.ContextRequested += OnCanvasContextRequested;
        }
        KeyDown += OnKeyDown;
        Focusable = true;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe(_viewModel);
        _viewModel = DataContext as UniverseCanvasViewModel;
        Subscribe(_viewModel);
        RenderScene();
    }

    private void Subscribe(UniverseCanvasViewModel? viewModel)
    {
        if (viewModel is null)
            return;

        viewModel.Nodes.CollectionChanged += OnSceneCollectionChanged;
        viewModel.Edges.CollectionChanged += OnSceneCollectionChanged;
    }

    private void Unsubscribe(UniverseCanvasViewModel? viewModel)
    {
        if (viewModel is null)
            return;

        viewModel.Nodes.CollectionChanged -= OnSceneCollectionChanged;
        viewModel.Edges.CollectionChanged -= OnSceneCollectionChanged;
    }

    private void OnSceneCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderScene();
    }

    private async void OnCanvasDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null || _canvas is null)
            return;

        var point = e.GetPosition(_canvas);
        await _viewModel.CreateNodeAtAsync(point.X, point.Y);
        RenderScene();
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_canvas is null || _scrollViewer is null || _viewModel is null)
            return;

        var source = e.Source as Control;
        var point = e.GetCurrentPoint(_canvas);
        _lastCanvasPoint = e.GetPosition(_canvas);

        if (point.Properties.IsRightButtonPressed)
        {
            if (_isDraggingNode || _isResizingNode)
                return;

            var taggedControl = FindTaggedAncestor(e.Source as Control);

            if (source is ShapePath edgePath && edgePath.Tag is CanvasRelationshipEdgeViewModel edge)
            {
                _viewModel?.SelectEdge(edge);
                ShowEdgeContextMenu(edgePath, edge);
                _suppressContextMenu = true;
                e.Handled = true;
                return;
            }

            if (taggedControl is Border border && border.Tag is CanvasEntityNodeViewModel node)
            {
                _viewModel?.SelectNode(node);
                ShowNodeContextMenu(border, node);
                _suppressContextMenu = true;
                e.Handled = true;
                return;
            }
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            _panOffset = _scrollViewer.Offset;
            e.Pointer.Capture(_canvas);
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            var taggedControl = FindTaggedAncestor(e.Source as Control);
            if (taggedControl is null && source is not ShapePath)
            {
                _viewModel.SelectEdge(null);
                _viewModel.SelectNode(null);
                RenderScene();
            }
        }

        Focus();
    }

    private void OnCanvasContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_canvas is null)
            return;

        if (_suppressContextMenu)
        {
            _suppressContextMenu = false;
            e.Handled = true;
            return;
        }

        var source = e.Source as Control;
        var taggedControl = FindTaggedAncestor(source);
        if (source is ShapePath edgePath && edgePath.Tag is CanvasRelationshipEdgeViewModel edge)
        {
            _viewModel?.SelectEdge(edge);
            ShowEdgeContextMenu(edgePath, edge);
            e.Handled = true;
            return;
        }

        if (taggedControl is Border border && border.Tag is CanvasEntityNodeViewModel node)
        {
            _viewModel?.SelectNode(node);
            ShowNodeContextMenu(border, node);
            e.Handled = true;
            return;
        }

        ShowCanvasContextMenu();
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _scrollViewer is not null)
        {
            var currentPan = e.GetPosition(this);
            var panDelta = currentPan - _panStart;
            _scrollViewer.Offset = new Vector(
                Math.Max(0, _panOffset.X - panDelta.X),
                Math.Max(0, _panOffset.Y - panDelta.Y));
            return;
        }

        if (_canvas is null)
            return;

        if (_viewModel is not null && _viewModel.IsLinkMode)
        {
            var previewPoint = e.GetPosition(_canvas);
            _viewModel.UpdateLinkPreview(previewPoint.X, previewPoint.Y);
            RenderScene();
        }

        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
            return;

        if (_resizeNode is not null)
        {
            var currentResize = e.GetPosition(_canvas);
            var resizeDeltaX = currentResize.X - _dragStart.X;
            var resizeDeltaY = currentResize.Y - _dragStart.Y;
            _resizeNode.Width = Math.Max(160, _originWidth + resizeDeltaX);
            _resizeNode.Height = Math.Max(100, _originHeight + resizeDeltaY);
            RenderScene();
            return;
        }

        if (_dragNode is null)
            return;

        var current = e.GetPosition(_canvas);
        var deltaX = current.X - _dragStart.X;
        var deltaY = current.Y - _dragStart.Y;

        _dragNode.X = Math.Max(12, _originX + deltaX);
        _dragNode.Y = Math.Max(12, _originY + deltaY);
        RenderScene();
    }

    private async void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_canvas is null)
            return;

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            return;
        }

        if (_dragNode is not null)
        {
            var node = _dragNode;
            _dragNode = null;
            _isDraggingNode = false;
            e.Pointer.Capture(null);
            if (_viewModel is not null)
                await _viewModel.PersistNodeLayoutAsync(node);
            RenderScene();
        }

        if (_resizeNode is not null)
        {
            var node = _resizeNode;
            _resizeNode = null;
            _isResizingNode = false;
            e.Pointer.Capture(null);
            if (_viewModel is not null)
                await _viewModel.PersistNodeLayoutAsync(node);
            RenderScene();
        }
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is null || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        if (e.Delta.Y > 0)
            _viewModel.ZoomInCommand.Execute(null);
        else if (e.Delta.Y < 0)
            _viewModel.ZoomOutCommand.Execute(null);
    }

    private async void OnSaveSelectedNodeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
            return;

        await _viewModel.SaveSelectedNodeAsync();
        RenderScene();
    }

    private async void OnSaveSelectedRelationshipClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel is null)
            return;

        await _viewModel.SaveSelectedRelationshipCommand.ExecuteAsync(null);
        RenderScene();
    }

    private void OnSelectedRelationshipTypeComboChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (sender is ComboBox comboBox && comboBox.SelectedItem is RelationshipType relationshipType)
        {
            _viewModel.SelectedNodeRelationshipType = relationshipType;
            _viewModel.SelectedNodeRelationshipTypeText = relationshipType.DisplayName;
        }
    }

    private async void OnDeleteSelectedRelationshipClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel?.SelectedEdge is null)
            return;

        await _viewModel.DeleteEdgeAsync(_viewModel.SelectedEdge);
        RenderScene();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.Key == Key.Delete)
        {
            if (_viewModel.SelectedEdge is not null)
            {
                _ = _viewModel.DeleteEdgeAsync(_viewModel.SelectedEdge);
            }
            else
            {
                _viewModel.DeleteSelectedNodeCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void RenderScene()
    {
        if (_canvas is null || _viewModel is null)
            return;

        if (_canvas.Parent is null)
            return;

        _canvas.Children.Clear();

        foreach (var edge in _viewModel.Edges)
        {
            var path = new ShapePath
            {
                Data = Geometry.Parse(edge.PathData),
                Stroke = new SolidColorBrush(Color.Parse(_viewModel.SelectedEdge?.Id == edge.Id ? "#ffd166" : "#5f7184")),
                StrokeThickness = _viewModel.SelectedEdge?.Id == edge.Id ? 3 : 2,
                Tag = edge
            };
            path.PointerPressed += OnEdgePointerPressed;
            _canvas.Children.Add(path);

            var label = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1a222b")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3),
                Child = new TextBlock
                {
                    Text = edge.Label,
                    FontSize = 11,
                    Foreground = Brushes.White
                }
            };

            Canvas.SetLeft(label, edge.LabelX);
            Canvas.SetTop(label, edge.LabelY);
            _canvas.Children.Add(label);
        }

        if (_viewModel.IsLinkMode && _viewModel.LinkSource is not null && _viewModel.HasLinkPreview)
        {
            var startX = _viewModel.LinkSource.X + (_viewModel.LinkSource.Width / 2);
            var startY = _viewModel.LinkSource.Y + (_viewModel.LinkSource.Height / 2);
            var endX = _viewModel.LinkPreviewX;
            var endY = _viewModel.LinkPreviewY;
            var control = Math.Max(48, Math.Min(140, Math.Abs(endX - startX) / 2));
            var previewPath = new ShapePath
            {
                Data = Geometry.Parse($"M {startX},{startY} C {startX + control},{startY} {endX - control},{endY} {endX},{endY}"),
                Stroke = new SolidColorBrush(Color.Parse("#c596ff")),
                StrokeThickness = 2,
                StrokeDashArray = new AvaloniaList<double> { 6, 4 }
            };
            _canvas.Children.Add(previewPath);
        }

        foreach (var node in _viewModel.Nodes)
        {
            var selected = _viewModel.SelectedNode?.Id == node.Id;
            var linking = _viewModel.LinkSource?.Id == node.Id;

            var border = new Border
            {
                Width = node.Width,
                Height = node.Height,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.Parse(linking ? "#3f2d61" : selected ? "#24445b" : "#1f252d")),
                BorderBrush = new SolidColorBrush(Color.Parse(linking ? "#c596ff" : selected ? "#6ec1ff" : "#465463")),
                BorderThickness = new Thickness(selected ? 2 : 1),
                Padding = new Thickness(10),
                ClipToBounds = true,
                Tag = node,
                Child = BuildNodeContent(node)
            };

            border.PointerPressed += OnNodePointerPressed;

            Canvas.SetLeft(border, node.X);
            Canvas.SetTop(border, node.Y);
            _canvas.Children.Add(border);

            var resizeHandle = new Border
            {
                Width = 14,
                Height = 14,
                Background = new SolidColorBrush(Color.Parse("#7b8ea1")),
                CornerRadius = new CornerRadius(3),
                BorderBrush = new SolidColorBrush(Color.Parse("#d0e3f7")),
                BorderThickness = new Thickness(1),
                Tag = node
            };
            resizeHandle.PointerPressed += OnResizeHandlePointerPressed;
            Canvas.SetLeft(resizeHandle, node.X + node.Width - 10);
            Canvas.SetTop(resizeHandle, node.Y + node.Height - 10);
            _canvas.Children.Add(resizeHandle);
        }
    }

    private async void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || _canvas is null || sender is not Border border || border.Tag is not CanvasEntityNodeViewModel node)
            return;

        if (e.Source is TextBox or ComboBox or Button)
            return;

        var point = e.GetCurrentPoint(border);
        if (point.Properties.IsRightButtonPressed)
        {
            _viewModel.SelectNode(node);
            ShowNodeContextMenu(border, node);
            RenderScene();
            return;
        }

        if (_viewModel.IsLinkMode)
        {
            await _viewModel.HandleNodeClickedAsync(node);
            RenderScene();
            return;
        }

        _viewModel.SelectNode(node);
        _dragNode = node;
        _isDraggingNode = true;
        _dragStart = e.GetPosition(_canvas);
        _originX = node.X;
        _originY = node.Y;
        e.Pointer.Capture(_canvas);
        e.Handled = true;
        RenderScene();
    }

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || _canvas is null || sender is not Border border || border.Tag is not CanvasEntityNodeViewModel node)
            return;

        if (!e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
            return;

        _viewModel.SelectNode(node);
        _resizeNode = node;
        _isResizingNode = true;
        _dragStart = e.GetPosition(_canvas);
        _originWidth = node.Width;
        _originHeight = node.Height;
        e.Pointer.Capture(_canvas);
        e.Handled = true;
        RenderScene();
    }

    private Control BuildNodeContent(CanvasEntityNodeViewModel node)
    {
        var isSelected = _viewModel?.SelectedNode?.Id == node.Id;
        if (isSelected && _viewModel is not null)
            return BuildSelectedNodeEditor(node);

        var layout = new Grid
        {
            ClipToBounds = true
        };
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var title = new TextBlock
        {
            Text = node.Name,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = Math.Max(40, node.Width - 56)
        };
        Grid.SetRow(title, 0);
        Grid.SetColumn(title, 0);
        layout.Children.Add(title);

        var deleteButton = new Button
        {
            Content = "×",
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        deleteButton.PointerPressed += (_, args) => args.Handled = true;
        deleteButton.Click += async (_, _) =>
        {
            if (_viewModel is not null)
            {
                await _viewModel.DeleteNodeAsync(node);
                RenderScene();
            }
        };
        Grid.SetRow(deleteButton, 0);
        Grid.SetColumn(deleteButton, 1);
        layout.Children.Add(deleteButton);

        var hydrateButton = new Button
        {
            Content = App.Current?.Resources.TryGetValue("entity.hydrate", out var hydrateText) == true
                ? hydrateText?.ToString()
                : "Hydrate",
            MinWidth = 68,
            Height = 22,
            Padding = new Thickness(6, 0)
        };
        hydrateButton.PointerPressed += (_, args) => args.Handled = true;
        hydrateButton.Click += async (_, _) =>
        {
            try
            {
                await HydrateNodeAsync(node);
            }
            catch (Exception ex)
            {
                if (_viewModel is not null)
                    _viewModel.StatusMessage = App.Current?.Resources.TryGetValue("ai.hydrate.failed", out var hydrateFailed) == true
                        ? string.Format(hydrateFailed?.ToString() ?? "Hydration failed: {0}", ex.Message)
                        : $"Hydration failed: {ex.Message}";
            }
        };
        Grid.SetRow(hydrateButton, 1);
        Grid.SetColumn(hydrateButton, 0);
        Grid.SetColumnSpan(hydrateButton, 2);
        hydrateButton.HorizontalAlignment = HorizontalAlignment.Left;
        hydrateButton.Margin = new Thickness(0, 4, 0, 0);
        layout.Children.Add(hydrateButton);

        var body = new Grid
        {
            ClipToBounds = true,
            Margin = new Thickness(0, 6, 0, 0)
        };
        body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        body.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        var typeBlock = new TextBlock
        {
            Text = node.TypeName,
            FontSize = 11,
            Opacity = 0.65,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = Math.Max(40, node.Width - 28)
        };
        Grid.SetRow(typeBlock, 0);
        body.Children.Add(typeBlock);

        var descriptionBlock = new TextBlock
        {
            Text = node.Description,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Width = Math.Max(60, node.Width - 28),
            MaxHeight = Math.Max(24, node.Height - 108),
            Opacity = 0.8,
            ClipToBounds = true
        };
        Grid.SetRow(descriptionBlock, 1);
        body.Children.Add(descriptionBlock);

        Grid.SetRow(body, 2);
        Grid.SetColumn(body, 0);
        Grid.SetColumnSpan(body, 2);
        layout.Children.Add(body);

        return layout;
    }

    private Control BuildSelectedNodeEditor(CanvasEntityNodeViewModel node)
    {
        var root = new Grid
        {
            ClipToBounds = true
        };
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var editorScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = Math.Max(92, node.Height - 40)
        };

        var layout = new StackPanel
        {
            Spacing = 6
        };
        editorScroll.Content = layout;
        Grid.SetRow(editorScroll, 0);
        root.Children.Add(editorScroll);

        TextBox? descriptionBox = null;
        TextBox? notesBox = null;
        var suppressInitialAutosave = true;
        var isSavingInlineNode = false;
        var pendingComboSave = false;
        var suppressHydrationAutosave = false;

        var nameBox = new TextBox
        {
            Text = _viewModel?.SelectedNodeName ?? node.Name,
            Watermark = "Name"
        };
        nameBox.PointerPressed += (_, args) => args.Handled = true;
        nameBox.GetObservable(TextBox.TextProperty).Subscribe(value =>
        {
            node.Entity.Name = string.IsNullOrWhiteSpace(value) ? "New Item" : value;
            node.RefreshDisplay();
            if (_viewModel is not null)
                _viewModel.SelectedNodeName = value ?? string.Empty;
        });
        nameBox.LostFocus += async (_, _) =>
        {
            if (suppressInitialAutosave || isSavingInlineNode)
                return;
            if (suppressHydrationAutosave)
                return;
            if (pendingComboSave)
                return;
            await SaveInlineNodeAsync();
        };
        nameBox.AttachedToVisualTree += (_, _) =>
        {
            nameBox.Focus();
            Dispatcher.UIThread.Post(() => suppressInitialAutosave = false, DispatcherPriority.Background);
        };
        nameBox.KeyDown += async (_, args) =>
        {
            if (_viewModel is null)
                return;

            if (args.Key == Key.Enter && !args.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                await SaveInlineNodeAsync();
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                _viewModel.SelectNode(null);
                RenderScene();
                args.Handled = true;
            }
        };
        layout.Children.Add(nameBox);

        var typeTextBox = new TextBox
        {
            Text = _viewModel?.SelectedNodeEntityTypeText ?? node.Entity.EntityType?.DisplayName ?? node.Entity.EntityType?.Name ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        typeTextBox.PointerPressed += (_, args) => args.Handled = true;
        typeTextBox.GetObservable(TextBox.TextProperty).Subscribe(value =>
        {
            if (_viewModel is not null)
                _viewModel.SelectedNodeEntityTypeText = value ?? string.Empty;
        });
        typeTextBox.LostFocus += async (_, _) =>
        {
            if (suppressInitialAutosave || isSavingInlineNode)
                return;
            if (suppressHydrationAutosave)
                return;
            await SaveInlineNodeAsync();
        };
        layout.Children.Add(typeTextBox);

        descriptionBox = new TextBox
        {
            Text = _viewModel?.SelectedNodeDescription ?? node.Description,
            Watermark = "Description",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = Math.Max(72, node.Height - 140)
        };
        descriptionBox.PointerPressed += (_, args) => args.Handled = true;
        descriptionBox.GetObservable(TextBox.TextProperty).Subscribe(value =>
        {
            node.Entity.Description = value ?? string.Empty;
            node.RefreshDisplay();
            if (_viewModel is not null)
                _viewModel.SelectedNodeDescription = value ?? string.Empty;
        });
        descriptionBox.LostFocus += async (_, _) =>
        {
            if (suppressInitialAutosave || isSavingInlineNode)
                return;
            if (suppressHydrationAutosave)
                return;
            if (pendingComboSave)
                return;
            await SaveInlineNodeAsync();
        };
        descriptionBox.KeyDown += async (_, args) =>
        {
            if (_viewModel is null)
                return;

            if ((args.Key == Key.Enter && args.KeyModifiers.HasFlag(KeyModifiers.Control)) ||
                (args.Key == Key.Enter && !args.KeyModifiers.HasFlag(KeyModifiers.Shift)))
            {
                await SaveInlineNodeAsync();
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                _viewModel.SelectNode(null);
                RenderScene();
                args.Handled = true;
            }
        };
        layout.Children.Add(descriptionBox);

        notesBox = new TextBox
        {
            Text = _viewModel?.SelectedNodeNotes ?? node.Entity.Notes ?? string.Empty,
            Watermark = "Notes",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 60
        };

        async Task SaveInlineNodeAsync()
        {
            if (_viewModel is null || isSavingInlineNode || descriptionBox is null || notesBox is null)
                return;

            isSavingInlineNode = true;
            try
            {
                await _viewModel.SaveNodeAsync(
                    node,
                    nameBox.Text,
                    descriptionBox.Text,
                    notesBox.Text,
                    _viewModel.SelectedNodeEntityType);
            }
            finally
            {
                isSavingInlineNode = false;
            }
        }

        notesBox.PointerPressed += (_, args) => args.Handled = true;
        notesBox.GetObservable(TextBox.TextProperty).Subscribe(value =>
        {
            node.Entity.Notes = value ?? string.Empty;
            if (_viewModel is not null)
                _viewModel.SelectedNodeNotes = value ?? string.Empty;
        });
        notesBox.LostFocus += async (_, _) =>
        {
            if (suppressInitialAutosave || isSavingInlineNode)
                return;
            if (suppressHydrationAutosave)
                return;
            if (pendingComboSave)
                return;
            await SaveInlineNodeAsync();
        };
        layout.Children.Add(notesBox);

        var hydrateButton = new Button
        {
            Content = App.Current?.Resources.TryGetValue("entity.hydrate", out var hydrateText) == true
                ? hydrateText?.ToString()
                : "Hydrate",
            MinWidth = 80
        };
        hydrateButton.PointerPressed += (_, args) => args.Handled = true;
        hydrateButton.Click += async (_, _) =>
        {
            try
            {
                suppressHydrationAutosave = true;
                await HydrateNodeAsync(node);
            }
            catch (Exception ex)
            {
                if (_viewModel is not null)
                    _viewModel.StatusMessage = App.Current?.Resources.TryGetValue("ai.hydrate.failed", out var hydrateFailed) == true
                        ? string.Format(hydrateFailed?.ToString() ?? "Hydration failed: {0}", ex.Message)
                        : $"Hydration failed: {ex.Message}";
            }
            finally
            {
                suppressHydrationAutosave = false;
            }
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 6, 0, 0)
        };
        buttons.Children.Add(hydrateButton);

        var deleteButton = new Button
        {
            Content = "×",
            Width = 26,
            Height = 26,
            Padding = new Thickness(0)
        };
        deleteButton.PointerPressed += (_, args) => args.Handled = true;
        deleteButton.Click += async (_, _) =>
        {
            if (_viewModel is not null)
            {
                await _viewModel.DeleteNodeAsync(node);
                RenderScene();
            }
        };
        buttons.Children.Add(deleteButton);

        Grid.SetRow(buttons, 1);
        root.Children.Add(buttons);
        return root;
    }

    private async Task<string?> ShowHydrationPromptDialogAsync(CanvasEntityNodeViewModel node)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return null;

        var promptBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 180,
            Width = 420,
            Watermark = App.Current?.Resources.TryGetValue("ai.hydrate.promptWatermark", out var promptWatermark) == true
                ? string.Format(promptWatermark?.ToString() ?? "Hydration prompt for {0}", node.Name)
                : $"Hydration prompt for {node.Name}"
        };

        string? result = null;
        var dialog = new Window
        {
            Title = App.Current?.Resources.TryGetValue("ai.hydrate.title", out var titleText) == true
                ? titleText?.ToString()
                : "Hydrate entity",
            Width = 500,
            Height = 330,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.Full,
            Topmost = true,
            ShowActivated = true
        };

        var cancelButton = new Button
        {
            Content = App.Current?.Resources.TryGetValue("ai.hydrate.cancel", out var cancelText) == true
                ? cancelText?.ToString()
                : "Cancel",
            MinWidth = 90
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var hydrateButton = new Button
        {
            Content = App.Current?.Resources.TryGetValue("entity.hydrate", out var hydrateButtonText) == true
                ? hydrateButtonText?.ToString()
                : "Hydrate",
            MinWidth = 90
        };
        hydrateButton.Click += (_, _) =>
        {
            result = promptBox.Text ?? string.Empty;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = App.Current?.Resources.TryGetValue("ai.hydrate.instructions", out var instructionsText) == true
                        ? string.Format(instructionsText?.ToString() ?? "Write the hydration prompt for '{0}'. Leave it empty to use a generic hydration.", node.Name)
                        : $"Write the hydration prompt for '{node.Name}'. Leave it empty to use a generic hydration.",
                    TextWrapping = TextWrapping.Wrap
                },
                promptBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelButton,
                        hydrateButton
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
        owner.Activate();
        owner.Focus();
        return result;
    }

    private async Task HydrateNodeAsync(CanvasEntityNodeViewModel node)
    {
        if (_viewModel is null)
            return;

        var prompt = await ShowHydrationPromptDialogAsync(node);
        if (prompt is null)
            return;

        _viewModel.SelectNode(node);
        var hydrated = await _viewModel.Hydration.HydrateCurrentNodeAsync(prompt, forceApplyDescription: true);
        if (!hydrated)
            return;

        if (_viewModel.SelectedNode?.Id == node.Id)
        {
            _viewModel.SelectedNodeDescription = node.Entity.Description ?? string.Empty;
            _viewModel.SelectedNodeNotes = node.Entity.Notes ?? string.Empty;
        }

        var refreshedEntity = await ScopedRunner.RunAsync<IEntityService, Entity>(
            _viewModel.ServiceProvider,
            service => service.GetByIdAsync(node.Id));
        if (refreshedEntity is null)
            return;

        refreshedEntity.EntityType = _viewModel.EntityTypes.FirstOrDefault(x => x.Id == refreshedEntity.EntityTypeId)
            ?? refreshedEntity.EntityType;

        node.Entity.Name = refreshedEntity.Name;
        node.Entity.Description = refreshedEntity.Description;
        node.Entity.Notes = refreshedEntity.Notes;
        node.Entity.HydrationData = refreshedEntity.HydrationData;
        node.Entity.ConfidenceLevel = refreshedEntity.ConfidenceLevel;
        node.Entity.CompletenessScore = refreshedEntity.CompletenessScore;
        node.Entity.EntityTypeId = refreshedEntity.EntityTypeId;
        node.Entity.EntityType = refreshedEntity.EntityType;
        node.RefreshDisplay();
        _viewModel.SelectNode(node);
        RenderScene();
    }

    private void ShowCanvasContextMenu()
    {
        if (_canvas is null || _viewModel is null)
            return;

        var menu = new ContextMenu();
        var items = new List<object>();

        var blankItem = new MenuItem
        {
            Header = App.Current?.Resources.TryGetValue("canvas.menu.createBlank", out var createBlank) == true ? createBlank?.ToString() : "Create blank item here"
        };
        blankItem.Click += async (_, _) =>
        {
            await _viewModel.CreateNodeAtAsync(_lastCanvasPoint.X, _lastCanvasPoint.Y);
            RenderScene();
        };
        items.Add(blankItem);

        var createHeader = new MenuItem
        {
            Header = App.Current?.Resources.TryGetValue("canvas.menu.createNew", out var createNew) == true ? createNew?.ToString() : "Create new item"
        };
        var createItems = new List<object>();
        foreach (var entityType in _viewModel.EntityTypes.OrderBy(x => x.Name))
        {
            var item = new MenuItem
            {
                Header = $"Create {entityType.DisplayName}"
            };
            item.Click += async (_, _) =>
            {
                await _viewModel.CreateNodeAtAsync(_lastCanvasPoint.X, _lastCanvasPoint.Y, entityType);
                RenderScene();
            };
            createItems.Add(item);
        }
        createHeader.ItemsSource = createItems;
        items.Add(createHeader);

        var existingHeader = new MenuItem
        {
            Header = App.Current?.Resources.TryGetValue("canvas.menu.addExisting", out var addExisting) == true ? addExisting?.ToString() : "Add existing item here"
        };
        var existingItems = new List<object>();
        foreach (var node in _viewModel.Nodes.OrderBy(x => x.Name))
        {
            var existingItem = new MenuItem
            {
                Header = node.Name
            };
            existingItem.Click += async (_, _) =>
            {
                await _viewModel.MoveExistingNodeAsync(node, _lastCanvasPoint.X, _lastCanvasPoint.Y);
                RenderScene();
            };
            existingItems.Add(existingItem);
        }
        existingHeader.ItemsSource = existingItems;
        items.Add(existingHeader);

        if (_viewModel.IsLinkMode)
        {
            var cancelLinkItem = new MenuItem
            {
                Header = App.Current?.Resources.TryGetValue("canvas.menu.cancelConnection", out var cancelConnection) == true ? cancelConnection?.ToString() : "Cancel connection mode"
            };
            cancelLinkItem.Click += (_, _) =>
            {
                _viewModel.CancelConnection();
                RenderScene();
            };
            items.Add(cancelLinkItem);
        }

        menu.ItemsSource = items;
        if (_canvasContextSurface is not null)
        {
            _canvasContextSurface.ContextMenu = menu;
            menu.Open(_canvasContextSurface);
            return;
        }

        _canvas.ContextMenu = menu;
        menu.Open(_canvas);
    }

    private void ShowNodeContextMenu(Control target, CanvasEntityNodeViewModel node)
    {
        if (_viewModel is null)
            return;

        var editItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.editNode", out var editNode) == true ? editNode?.ToString() : "Edit this node in panel" };
        editItem.Click += (_, _) =>
        {
            _viewModel.SelectNode(node);
            RenderScene();
        };

        var deleteItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.deleteNode", out var deleteNode) == true ? deleteNode?.ToString() : "Delete node" };
        deleteItem.Click += async (_, _) =>
        {
            await _viewModel.DeleteNodeAsync(node);
            RenderScene();
        };

        var connectItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.startConnection", out var startConnection) == true ? startConnection?.ToString() : "Start connection from this node" };
        connectItem.Click += (_, _) =>
        {
            _viewModel.StartConnection(node);
            RenderScene();
        };

        var hydrateItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.hydrateEntity", out var hydrateEntity) == true ? hydrateEntity?.ToString() : "Hydrate this entity" };
        hydrateItem.Click += async (_, _) =>
        {
            await HydrateNodeAsync(node);
        };

        var cancelConnectionItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.cancelConnection", out var cancelNodeConnection) == true ? cancelNodeConnection?.ToString() : "Cancel connection mode" };
        cancelConnectionItem.Click += (_, _) =>
        {
            _viewModel.CancelConnection();
            RenderScene();
        };

        var menu = new ContextMenu
        {
            ItemsSource = new object[] { editItem, connectItem, hydrateItem, cancelConnectionItem, deleteItem }
        };
        target.ContextMenu = menu;
        menu.Open(target);
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || sender is not ShapePath path || path.Tag is not CanvasRelationshipEdgeViewModel edge)
            return;

        if (e.ClickCount >= 2)
        {
            _ = _viewModel.DeleteEdgeAsync(edge);
            e.Handled = true;
            return;
        }

        if (e.GetCurrentPoint(path).Properties.IsRightButtonPressed)
        {
            _viewModel.SelectEdge(edge);
            ShowEdgeContextMenu(path, edge);
            RenderScene();
            e.Handled = true;
            return;
        }

        _viewModel.SelectEdge(edge);
        RenderScene();
        e.Handled = true;
    }

    private void ShowEdgeContextMenu(Control target, CanvasRelationshipEdgeViewModel edge)
    {
        if (_viewModel is null)
            return;

        var selectItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.editRelationship", out var editRelationship) == true ? editRelationship?.ToString() : "Edit relationship in panel" };
        selectItem.Click += (_, _) =>
        {
            _viewModel.SelectEdge(edge);
            RenderScene();
        };

        var typeHeader = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.changeRelationshipType", out var changeRelationshipType) == true ? changeRelationshipType?.ToString() : "Change relationship type" };
        var typeItems = new List<object>();
        foreach (var relationshipType in _viewModel.RelationshipTypes.OrderBy(x => x.Name))
        {
            var typeItem = new MenuItem { Header = relationshipType.Name };
            typeItem.Click += async (_, _) =>
            {
                await _viewModel.UpdateEdgeTypeAsync(edge, relationshipType);
                RenderScene();
            };
            typeItems.Add(typeItem);
        }
        typeHeader.ItemsSource = typeItems;

        var deleteItem = new MenuItem { Header = App.Current?.Resources.TryGetValue("canvas.menu.deleteRelationship", out var deleteRelationship) == true ? deleteRelationship?.ToString() : "Delete relationship" };
        deleteItem.Click += async (_, _) =>
        {
            await _viewModel.DeleteEdgeAsync(edge);
            RenderScene();
        };

        var menu = new ContextMenu
        {
            ItemsSource = new object[] { selectItem, typeHeader, deleteItem }
        };
        target.ContextMenu = menu;
        menu.Open(target);
    }

    private static Control? FindTaggedAncestor(Control? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Border border && border.Tag is CanvasEntityNodeViewModel)
                return border;
            current = current.Parent as Control;
        }

        return null;
    }
}