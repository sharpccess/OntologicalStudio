using Avalonia.Controls;
using Avalonia.Threading;

namespace OntologicalStudio.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowInTaskbar = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = true;
        Opened += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;
                Activate();
                Focus();
            });
        };
    }
}
