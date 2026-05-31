using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using OntologicalStudio.Desktop.ViewModels;
using System.Collections.Specialized;
using ShapePath = Avalonia.Controls.Shapes.Path;
using MenuItem = Avalonia.Controls.MenuItem;

namespace OntologicalStudio.Desktop.Views;

public partial class UniverseCanvasView : UserControl
{
    private Canvas? _canvas;
    private ScrollViewer? _scrollViewer;
    private UniverseCanvasViewModel? _viewModel;
    private CanvasEntityNodeViewModel? _dragNode;
    private Point _dragStart;
    private double _originX;
    private double _originY;
    private bool _isPanning;
    private Point _panStart;
    private Vector _panOffset;
    private Point _lastCanvasPoint;

    public UniverseCanvasView()
    {
        AvaloniaXamlLoader.Load(this);
        _canvas = this.FindControl<Canvas>("CanvasHost");
        _scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollHost");
        DataContextChanged += OnDataContextChanged;
        if (_canvas is not null)
        {
            _canvas.DoubleTapped += OnCanvasDoubleTapped;
            _canvas.PointerPressed += OnCanvasPointerPressed;
            _canvas.PointerMoved += OnCanvasPointerMoved;
            _canvas.PointerReleased += OnCanvasPointerReleased;
            _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
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
        if (_canvas is null || _scrollViewer is null)
            return;

        var point = e.GetCurrentPoint(_canvas);
        _lastCanvasPoint = e.GetPosition(_canvas);

        if (point.Properties.IsRightButtonPressed)
        {
            ShowCanvasContextMenu();
            Focus();
            return;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            _panOffset = _scrollViewer.Offset;
            e.Pointer.Capture(_canvas);
        }
        Focus();
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

        if (_canvas is null || _dragNode is null)
            return;

        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
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
            e.Pointer.Capture(null);
            if (_viewModel is not null)
                await _viewModel.PersistNodePositionAsync(node);
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.Key == Key.Delete)
        {
            _viewModel.DeleteSelectedNodeCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void RenderScene()
    {
        if (_canvas is null || _viewModel is null)
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
                Tag = node,
                Child = BuildNodeContent(node)
            };

            border.PointerPressed += OnNodePointerPressed;

            Canvas.SetLeft(border, node.X);
            Canvas.SetTop(border, node.Y);
            _canvas.Children.Add(border);
        }
    }

    private async void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || _canvas is null || sender is not Border border || border.Tag is not CanvasEntityNodeViewModel node)
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
        _dragStart = e.GetPosition(_canvas);
        _originX = node.X;
        _originY = node.Y;
        e.Pointer.Capture(_canvas);
        RenderScene();
    }

    private Control BuildNodeContent(CanvasEntityNodeViewModel node)
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        layout.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var title = new TextBlock
        {
            Text = node.Name,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
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

        var body = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = node.TypeName,
                    FontSize = 11,
                    Opacity = 0.65
                },
                new TextBlock
                {
                    Text = node.Description,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 32,
                    Opacity = 0.8
                }
            }
        };
        Grid.SetRow(body, 1);
        Grid.SetColumn(body, 0);
        Grid.SetColumnSpan(body, 2);
        layout.Children.Add(body);

        return layout;
    }

    private void ShowCanvasContextMenu()
    {
        if (_canvas is null || _viewModel is null)
            return;

        var menu = new ContextMenu();
        var items = new List<object>();

        var createHeader = new MenuItem
        {
            Header = "Create new item"
        };
        var createItems = new List<object>();
        foreach (var entityType in _viewModel.EntityTypes.OrderBy(x => x.Name))
        {
            var item = new MenuItem
            {
                Header = $"Create {entityType.Name}"
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
            Header = "Add existing item here"
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

        menu.ItemsSource = items;
        menu.Open(_canvas);
    }

    private void ShowNodeContextMenu(Control target, CanvasEntityNodeViewModel node)
    {
        if (_viewModel is null)
            return;

        var editItem = new MenuItem { Header = "Edit this node in panel" };
        editItem.Click += (_, _) =>
        {
            _viewModel.SelectNode(node);
            RenderScene();
        };

        var deleteItem = new MenuItem { Header = "Delete node" };
        deleteItem.Click += async (_, _) =>
        {
            await _viewModel.DeleteNodeAsync(node);
            RenderScene();
        };

        var connectItem = new MenuItem { Header = "Start connection from this node" };
        connectItem.Click += (_, _) =>
        {
            _viewModel.StartConnection(node);
            RenderScene();
        };

        var cancelConnectionItem = new MenuItem { Header = "Cancel connection mode" };
        cancelConnectionItem.Click += (_, _) =>
        {
            _viewModel.CancelConnection();
            RenderScene();
        };

        var menu = new ContextMenu
        {
            ItemsSource = new object[] { editItem, connectItem, cancelConnectionItem, deleteItem }
        };
        menu.Open(target);
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || sender is not ShapePath path || path.Tag is not CanvasRelationshipEdgeViewModel edge)
            return;

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

        var selectItem = new MenuItem { Header = "Edit relationship in panel" };
        selectItem.Click += (_, _) =>
        {
            _viewModel.SelectEdge(edge);
            RenderScene();
        };

        var typeHeader = new MenuItem { Header = "Change relationship type" };
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

        var deleteItem = new MenuItem { Header = "Delete relationship" };
        deleteItem.Click += async (_, _) =>
        {
            await _viewModel.DeleteEdgeAsync(edge);
            RenderScene();
        };

        var menu = new ContextMenu
        {
            ItemsSource = new object[] { selectItem, typeHeader, deleteItem }
        };
        menu.Open(target);
    }
}