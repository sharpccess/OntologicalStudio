using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Desktop.ViewModels;
using OntologicalStudio.Desktop.Views;
using OntologicalStudio.Infrastructure;
using OntologicalStudio.Localization.Services;
using OntologicalStudio.Persistence.Context;
using System;
using System.IO;
using System.Text;

namespace OntologicalStudio.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OntologicalStudio",
        "startup.log");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        WriteStartupLog("OnFrameworkInitializationCompleted start");
        try
        {
            Services = ConfigureServices();
            WriteStartupLog("Services configured");

            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ctx.Database.Migrate();
            DatabaseSeeder.SeedAsync(ctx).GetAwaiter().GetResult();
            WriteStartupLog("Database migrated + seeded");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Startup] App service init failed: {ex.Message}");
            WriteStartupLog($"App service init failed: {ex}");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                if (Services is null)
                {
                    desktop.MainWindow = BuildFallbackWindow("Services not initialized.");
                    WriteStartupLog("Fallback window created: services null");
                }
                else
                {
                    var window = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(Services)
                    };
                    desktop.MainWindow = window;
                    WriteStartupLog("MainWindow created");
                }
            }
            catch (Exception ex)
            {
                WriteStartupLog($"MainWindow creation failed: {ex}");
                desktop.MainWindow = BuildFallbackWindow(ex.ToString());
            }
        }

        WriteStartupLog("OnFrameworkInitializationCompleted end");
        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OntologicalStudio");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "ontology.db");
        var connectionString = $"Data Source={dbPath}";

        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString);
        services.AddLocalization(Path.Combine(AppContext.BaseDirectory, "Languages"));
        return services.BuildServiceProvider();
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(StartupLogPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static Window BuildFallbackWindow(string message)
    {
        return new Window
        {
            Title = "Ontological Studio - Startup Error",
            Width = 900,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new TextBox
            {
                Text = message,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
    }
}
