using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OntologicalStudio.Desktop.Views;

public partial class AiOperationOverlay : UserControl
{
    public AiOperationOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
