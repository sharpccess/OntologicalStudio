using System;

namespace OntologicalStudio.Desktop.ViewModels;

public class MainWindowViewModel
{
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
}
