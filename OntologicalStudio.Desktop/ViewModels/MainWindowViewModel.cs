using CommunityToolkit.Mvvm.ComponentModel;
using OntologicalStudio.Localization.Services;
using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

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

    public ObservableCollection<string> AvailableLanguages { get; } = new();

    public UniversesViewModel Universes { get; }
    public UniverseCanvasViewModel UniverseCanvas { get; }
    public EntitiesViewModel Entities { get; }
    public RelationshipsViewModel Relationships { get; }
    public ScenariosViewModel Scenarios { get; }
    public PromptPreviewViewModel Prompt { get; }

    public MainWindowViewModel(IServiceProvider provider)
    {
        _localization = provider.GetRequiredService<ILocalizationService>();
        _localization.OnLanguageChanged += ApplyLocalization;

        foreach (var language in _localization.GetAvailableLanguages().OrderBy(x => x))
            AvailableLanguages.Add(language);
        SelectedLanguageCode = _localization.CurrentLanguageCode;

        Universes = new UniversesViewModel(provider);
        UniverseCanvas = new UniverseCanvasViewModel(provider, Universes);
        Entities = new EntitiesViewModel(provider, Universes);
        Relationships = new RelationshipsViewModel(provider, Universes);
        Scenarios = new ScenariosViewModel(provider, Universes);
        Prompt = new PromptPreviewViewModel(provider, Universes);
        ApplyLocalization();

        // Initial load
        _ = Universes.LoadAsync();
        _ = LoadDeferredAsync();
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
            1 => UniverseCanvas.LoadAsync(),
            2 => Entities.LoadAsync(),
            3 => Relationships.ReloadForUniverseAsync(),
            4 => Scenarios.LoadAsync(),
            5 => Prompt.ReloadScenariosAsync(),
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
    }

    private async Task LoadDeferredAsync()
    {
        await Task.Delay(50);
        await UniverseCanvas.LoadAsync();
        await Entities.LoadAsync();
        await Relationships.ReloadForUniverseAsync();
        await Scenarios.LoadAsync();
        await Prompt.ReloadScenariosAsync();
    }
}
