using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Desktop.ViewModels;
using OntologicalStudio.Desktop.Views;
using OntologicalStudio.Infrastructure;
using OntologicalStudio.Persistence.Context;
using System;
using System.IO;

namespace OntologicalStudio.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        // Apply migrations + seed once at startup
        try
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ctx.Database.Migrate();
            DatabaseSeeder.SeedAsync(ctx).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Startup] DB init failed: {ex.Message}");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(Services)
            };
        }

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
        return services.BuildServiceProvider();
    }
}
