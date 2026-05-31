using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int selectedTabIndex;

    public UniversesViewModel Universes { get; }
    public UniverseCanvasViewModel UniverseCanvas { get; }
    public EntitiesViewModel Entities { get; }
    public RelationshipsViewModel Relationships { get; }
    public ScenariosViewModel Scenarios { get; }
    public PromptPreviewViewModel Prompt { get; }

    public MainWindowViewModel(IServiceProvider provider)
    {
        Universes = new UniversesViewModel(provider);
        UniverseCanvas = new UniverseCanvasViewModel(provider, Universes);
        Entities = new EntitiesViewModel(provider, Universes);
        Relationships = new RelationshipsViewModel(provider, Universes);
        Scenarios = new ScenariosViewModel(provider, Universes);
        Prompt = new PromptPreviewViewModel(provider, Universes);

        // Initial load
        _ = Universes.LoadAsync();
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
}
