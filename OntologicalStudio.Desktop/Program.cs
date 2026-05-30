using Avalonia;
using Avalonia.ReactiveUI;

namespace OntologicalStudio.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<AppMainClass>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }
}
