using OntologicalStudio.Core.Models;
using ReactiveUI;

namespace OntologicalStudio.Desktop.ViewModels;

public class ScenarioViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _goals = string.Empty;
    private ScenarioStatus _status;

    public Scenario Model { get; }

    public Guid Id => Model.Id;

    public string Title
    {
        get => _title;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _title, value))
            {
                Model.Title = value;
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _description, value))
            {
                Model.Description = value;
            }
        }
    }

    public string Goals
    {
        get => _goals;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _goals, value))
            {
                Model.Goals = value;
            }
        }
    }

    public ScenarioStatus Status
    {
        get => _status;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _status, value))
            {
                Model.Status = value;
            }
        }
    }

    public ScenarioViewModel(Scenario model)
    {
        Model = model;
        _title = model.Title;
        _description = model.Description;
        _goals = model.Goals ?? string.Empty;
        _status = model.Status;
    }
}
