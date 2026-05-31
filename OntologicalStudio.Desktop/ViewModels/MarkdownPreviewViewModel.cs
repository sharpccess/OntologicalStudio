using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class MarkdownPreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private double fontSize = 12;

    [ObservableProperty]
    private FontWeight fontWeight = FontWeight.Normal;

    [ObservableProperty]
    private bool isCode;

    [ObservableProperty]
    private Thickness margin = new(0, 0, 0, 6);

    [ObservableProperty]
    private Thickness padding = new(0);

    [ObservableProperty]
    private Thickness borderThickness = new(0);

    [ObservableProperty]
    private IBrush foreground = Brushes.White;

    [ObservableProperty]
    private IBrush background = Brushes.Transparent;

    [ObservableProperty]
    private IBrush borderBrush = Brushes.Transparent;

    [ObservableProperty]
    private FontFamily fontFamily = new("Segoe UI");
}