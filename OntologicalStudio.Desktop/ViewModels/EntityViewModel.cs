using OntologicalStudio.Core.Models;
using ReactiveUI;

namespace OntologicalStudio.Desktop.ViewModels;

public class EntityViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private double _positionX;
    private double _positionY;
    private int _confidenceLevel;
    private int _completenessScore;
    private string _notes = string.Empty;

    public Entity Model { get; }

    public Guid Id => Model.Id;

    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            Model.Name = value;
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            this.RaiseAndSetIfChanged(ref _description, value);
            Model.Description = value;
        }
    }

    public double PositionX
    {
        get => _positionX;
        set
        {
            this.RaiseAndSetIfChanged(ref _positionX, value);
            Model.PositionX = value;
        }
    }

    public double PositionY
    {
        get => _positionY;
        set
        {
            this.RaiseAndSetIfChanged(ref _positionY, value);
            Model.PositionY = value;
        }
    }

    public int ConfidenceLevel
    {
        get => _confidenceLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _confidenceLevel, value);
            Model.ConfidenceLevel = value;
        }
    }

    public int CompletenessScore
    {
        get => _completenessScore;
        set
        {
            this.RaiseAndSetIfChanged(ref _completenessScore, value);
            Model.CompletenessScore = value;
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            this.RaiseAndSetIfChanged(ref _notes, value);
            Model.Notes = value;
        }
    }

    public string TypeName => Model.EntityType?.Name ?? "General";

    public EntityViewModel(Entity model)
    {
        Model = model;
        _name = model.Name;
        _description = model.Description;
        _positionX = model.PositionX;
        _positionY = model.PositionY;
        _confidenceLevel = model.ConfidenceLevel;
        _completenessScore = model.CompletenessScore;
        _notes = model.Notes ?? string.Empty;
    }
}
