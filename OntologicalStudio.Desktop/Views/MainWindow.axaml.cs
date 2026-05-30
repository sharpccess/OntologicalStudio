using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using OntologicalStudio.Desktop.ViewModels;
using System;
using System.Reactive.Disposables;

namespace OntologicalStudio.Desktop.Views;

public partial class MainWindow : Window, IDisposable
{
    // Drag and drop state
    private bool _isDraggingNode;
    private EntityViewModel? _draggedEntity;
    private Point _draggedEntityStartPos;
    private Point _draggedNodeStartPointerPos;

    // Pan state
    private bool _isPanning;
    private Point _panStartOffset;
    private Point _panStartPointerPos;

    // Transform references
    private TransformGroup? _canvasTransformGroup;
    private ScaleTransform? _canvasScale;
    private TranslateTransform? _canvasTranslate;

    private CompositeDisposable? _disposables;

    public MainWindow()
    {
        InitializeComponent();
        _disposables = new CompositeDisposable();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        
        // Find transforms after XAML is loaded
        var canvasContainer = this.FindControl<Panel>("CanvasContainer");
        if (canvasContainer?.RenderTransform is TransformGroup transformGroup)
        {
            _canvasTransformGroup = transformGroup;
            
            // Extract ScaleTransform and TranslateTransform from the group
            if (transformGroup.Children.Count >= 2)
            {
                _canvasScale = transformGroup.Children[0] as ScaleTransform;
                _canvasTranslate = transformGroup.Children[1] as TranslateTransform;
            }
        }
    }

    // Node drag handlers
    private void OnNodePointerPressed(object sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed) return;

        if (sender is Border border && border.DataContext is EntityViewModel entityVm)
        {
            _isDraggingNode = true;
            _draggedEntity = entityVm;
            
            // Set as selected object in ViewModel
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SelectedObject = entityVm;
            }

            // Record pointer start relative to the container canvas
            var canvasContainer = this.FindControl<Panel>("CanvasContainer");
            if (canvasContainer != null)
            {
                _draggedNodeStartPointerPos = e.GetPosition(canvasContainer);
                _draggedEntityStartPos = new Point(entityVm.PositionX, entityVm.PositionY);
            }

            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OnNodePointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDraggingNode && _draggedEntity != null && sender is Border border)
        {
            var canvasContainer = this.FindControl<Panel>("CanvasContainer");

            if (canvasContainer != null && _canvasScale != null)
            {
                var currentPointerPos = e.GetPosition(canvasContainer);
                
                // Account for zoom level in drag displacement
                double deltaX = (currentPointerPos.X - _draggedNodeStartPointerPos.X) / _canvasScale.ScaleX;
                double deltaY = (currentPointerPos.Y - _draggedNodeStartPointerPos.Y) / _canvasScale.ScaleY;

                _draggedEntity.PositionX = Math.Max(0, _draggedEntityStartPos.X + deltaX);
                _draggedEntity.PositionY = Math.Max(0, _draggedEntityStartPos.Y + deltaY);
            }
            e.Handled = true;
        }
    }

    private void OnNodePointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingNode)
        {
            if (sender is Border border)
            {
                e.Pointer.Capture(null);
            }

            _isDraggingNode = false;
            _draggedEntity = null;

            // Save new coordinates to database
            if (DataContext is MainWindowViewModel vm)
            {
                _ = vm.SaveSelectedObjectAsync();
            }
            e.Handled = true;
        }
    }

    // Canvas pan and zoom handlers
    private void OnCanvasPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        
        // Pan with right-click or middle-click on empty canvas
        if (properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStartPointerPos = e.GetPosition(this);

            if (_canvasTranslate != null)
            {
                _panStartOffset = new Point(_canvasTranslate.X, _canvasTranslate.Y);
            }

            var canvasContainer = this.FindControl<Panel>("CanvasContainer");
            if (canvasContainer != null)
            {
                e.Pointer.Capture(canvasContainer);
            }
            
            // Clear selection when clicking canvas background
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SelectedObject = null;
            }
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerEventArgs e)
    {
        if (_isPanning && _canvasTranslate != null)
        {
            var currentPointerPos = e.GetPosition(this);
            double deltaX = currentPointerPos.X - _panStartPointerPos.X;
            double deltaY = currentPointerPos.Y - _panStartPointerPos.Y;

            _canvasTranslate.X = _panStartOffset.X + deltaX;
            _canvasTranslate.Y = _panStartOffset.Y + deltaY;
        }
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            var canvasContainer = this.FindControl<Panel>("CanvasContainer");
            if (canvasContainer != null)
            {
                e.Pointer.Capture(null);
            }
            _isPanning = false;
            e.Handled = true;
        }
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (_canvasScale != null)
        {
            double zoomFactor = e.Delta.Y > 0 ? 1.15 : 0.85;
            double newScaleX = _canvasScale.ScaleX * zoomFactor;
            double newScaleY = _canvasScale.ScaleY * zoomFactor;

            // Clamp zoom levels
            _canvasScale.ScaleX = Math.Clamp(newScaleX, 0.2, 3.0);
            _canvasScale.ScaleY = Math.Clamp(newScaleY, 0.2, 3.0);

            e.Handled = true;
        }
    }

    // PDF Generation click
    private void OnGenerateReportClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.SelectedUniverse != null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Consulting Report PDF",
                DefaultExtension = "pdf",
                SuggestedFileName = $"{vm.SelectedUniverse.Name}_Consulting_Report.pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Documents") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (file != null)
            {
                string filePath = file.Path.LocalPath;
                vm.ExportPdfReportCommand.Execute(filePath).Subscribe();
            }
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();
        _disposables = null;
    }
}
