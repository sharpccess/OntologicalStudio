using Avalonia;
using OntologicalStudio.Core.Models;
using ReactiveUI;
using System;

namespace OntologicalStudio.Desktop.ViewModels;

public class RelationshipViewModel : ViewModelBase
{
    public Relationship Model { get; }
    public EntityViewModel Source { get; }
    public EntityViewModel Target { get; }

    public Guid Id => Model.Id;
    public string TypeName => Model.RelationshipType?.Name ?? "influences";
    public string Description => Model.Description ?? string.Empty;

    // Center coordinates computation (assuming node size of 180x90)
    public double StartX => Source.PositionX + 90;
    public double StartY => Source.PositionY + 45;
    public double EndX => Target.PositionX + 90;
    public double EndY => Target.PositionY + 45;

    public Point StartPoint => new Point(StartX, StartY);
    public Point EndPoint => new Point(EndX, EndY);

    public double LabelX => (StartX + EndX) / 2;
    public double LabelY => (StartY + EndY) / 2 - 12;

    public RelationshipViewModel(Relationship model, EntityViewModel source, EntityViewModel target)
    {
        Model = model;
        Source = source;
        Target = target;

        // Listen to source property coordinates changed
        Source.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EntityViewModel.PositionX) || e.PropertyName == nameof(EntityViewModel.PositionY))
            {
                this.RaisePropertyChanged(nameof(StartX));
                this.RaisePropertyChanged(nameof(StartY));
                this.RaisePropertyChanged(nameof(StartPoint));
                this.RaisePropertyChanged(nameof(LabelX));
                this.RaisePropertyChanged(nameof(LabelY));
            }
        };

        // Listen to target property coordinates changed
        Target.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EntityViewModel.PositionX) || e.PropertyName == nameof(EntityViewModel.PositionY))
            {
                this.RaisePropertyChanged(nameof(EndX));
                this.RaisePropertyChanged(nameof(EndY));
                this.RaisePropertyChanged(nameof(EndPoint));
                this.RaisePropertyChanged(nameof(LabelX));
                this.RaisePropertyChanged(nameof(LabelY));
            }
        };
    }
}
