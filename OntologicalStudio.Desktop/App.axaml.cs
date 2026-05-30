using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Desktop.ViewModels;
using OntologicalStudio.Desktop.Views;
using OntologicalStudio.Infrastructure;
using OntologicalStudio.Persistence.Context;
using System;
using System.IO;

namespace OntologicalStudio.Desktop;

public class AppMainClass : Avalonia.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Database connection string setup in LocalApplicationData
        string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OntologicalStudio");
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }
        string dbPath = Path.Combine(appDataDir, "ontological_studio.db");
        string connectionString = $"Data Source={dbPath}";

        services.AddInfrastructure(connectionString);

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        // Migrate and seed database
        using (var scope = ServiceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DatabaseSeeder.SeedAsync(context).GetAwaiter().GetResult();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
