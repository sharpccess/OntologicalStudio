using CommunityToolkit.Mvvm.ComponentModel;
using OntologicalStudio.Localization.Services;
using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Threading;
using System.IO;
using System.Text;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly IServiceProvider _provider;
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OntologicalStudio",
        "startup.log");

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private string appTitle = "Ontological Studio";

    [ObservableProperty]
    private string appSubtitle = "Ontology-Augmented Reasoning System";

    [ObservableProperty]
    private string universesTabHeader = "Universes";

    [ObservableProperty]
    private string canvasTabHeader = "Universe Canvas";

    [ObservableProperty]
    private string entitiesTabHeader = "Entities";

    [ObservableProperty]
    private string relationshipsTabHeader = "Relationships";

    [ObservableProperty]
    private string scenariosTabHeader = "Scenarios";

    [ObservableProperty]
    private string promptPreviewTabHeader = "Prompt Preview";

    [ObservableProperty]
    private string languageLabel = "Language";

    [ObservableProperty]
    private string selectedLanguageCode = "en";

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new();

    [ObservableProperty]
    private UniversesViewModel? universes;

    [ObservableProperty]
    private UniverseCanvasViewModel? universeCanvas;

    [ObservableProperty]
    private EntitiesViewModel? entities;

    [ObservableProperty]
    private RelationshipsViewModel? relationships;

    [ObservableProperty]
    private ScenariosViewModel? scenarios;

    [ObservableProperty]
    private PromptPreviewViewModel? prompt;

    public MainWindowViewModel(IServiceProvider provider)
    {
        _provider = provider;
        WriteStartupLog("MainWindowViewModel ctor start");
        _localization = provider.GetRequiredService<ILocalizationService>();
        WriteStartupLog("MainWindowViewModel localization service resolved");
        _localization.OnLanguageChanged += ApplyLocalization;
        WriteStartupLog("MainWindowViewModel localization event wired");

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                WriteStartupLog("MainWindowViewModel deferred init scheduled");
                await InitializeShellAsync();
            }
            catch (Exception ex)
            {
                WriteStartupLog($"MainWindowViewModel InitializeShellAsync error: {ex}");
            }
        }, DispatcherPriority.Background);

        WriteStartupLog("MainWindowViewModel ctor end");
    }

    partial void OnSelectedLanguageCodeChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == _localization.CurrentLanguageCode)
            return;

        _localization.ChangeLanguage(value);
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        _ = value switch
        {
            1 => UniverseCanvas?.LoadAsync() ?? Task.CompletedTask,
            2 => Entities?.LoadAsync() ?? Task.CompletedTask,
            3 => Relationships?.ReloadForUniverseAsync() ?? Task.CompletedTask,
            4 => Scenarios?.LoadAsync() ?? Task.CompletedTask,
            5 => Prompt?.ReloadScenariosAsync() ?? Task.CompletedTask,
            _ => Task.CompletedTask
        };
    }

    private void ApplyLocalization()
    {
        AppTitle = _localization.T("app.title");
        AppSubtitle = _localization.T("app.subtitle");
        LanguageLabel = _localization.T("app.language");
        UniversesTabHeader = _localization.T("tab.universes");
        CanvasTabHeader = _localization.T("tab.canvas");
        EntitiesTabHeader = _localization.T("tab.entities");
        RelationshipsTabHeader = _localization.T("tab.relationships");
        ScenariosTabHeader = _localization.T("tab.scenarios");
        PromptPreviewTabHeader = _localization.T("tab.promptPreview");

        if (App.Current?.Resources is { } resources)
        {
            foreach (var entry in _localization.GetLoadedTranslations())
            {
                resources[entry.Key] = entry.Value;
            }
        }
    }

    private async Task InitializeShellAsync()
    {
        try
        {
            WriteStartupLog("MainWindowViewModel InitializeShellAsync start");
            await Task.Delay(100);

            foreach (var language in _localization.GetAvailableLanguages().OrderBy(x => x))
                AvailableLanguages.Add(new LanguageOption(language, language.ToUpperInvariant()));
            SelectedLanguageCode = _localization.CurrentLanguageCode;
            ApplyLocalization();
            WriteStartupLog("MainWindowViewModel localization initialized");

            var universes = new UniversesViewModel(_provider);
            WriteStartupLog("UniversesViewModel created");
            var canvas = new UniverseCanvasViewModel(_provider, universes);
            WriteStartupLog("UniverseCanvasViewModel created");
            var entities = new EntitiesViewModel(_provider, universes);
            WriteStartupLog("EntitiesViewModel created");
            var relationships = new RelationshipsViewModel(_provider, universes);
            WriteStartupLog("RelationshipsViewModel created");
            var scenarios = new ScenariosViewModel(_provider, universes);
            WriteStartupLog("ScenariosViewModel created");
            var prompt = new PromptPreviewViewModel(_provider, universes);
            WriteStartupLog("PromptPreviewViewModel created");

            Universes = universes;
            UniverseCanvas = canvas;
            Entities = entities;
            Relationships = relationships;
            Scenarios = scenarios;
            Prompt = prompt;

            await Universes.LoadAsync();
            WriteStartupLog("MainWindowViewModel Universes loaded");

            await Task.Delay(100);
            await UniverseCanvas.LoadAsync();
            WriteStartupLog("MainWindowViewModel Canvas loaded");

            await Task.Delay(100);
            await Entities.LoadAsync();
            WriteStartupLog("MainWindowViewModel Entities loaded");

            await Task.Delay(100);
            await Relationships.ReloadForUniverseAsync();
            WriteStartupLog("MainWindowViewModel Relationships loaded");

            await Task.Delay(100);
            await Scenarios.LoadAsync();
            WriteStartupLog("MainWindowViewModel Scenarios loaded");

            await Task.Delay(100);
            await Prompt.ReloadScenariosAsync();
            WriteStartupLog("MainWindowViewModel Prompt loaded");
        }
        catch (Exception ex)
        {
            WriteStartupLog($"MainWindowViewModel InitializeShellAsync exception: {ex}");
        }
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
}

public record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}
