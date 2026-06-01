using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Core.Interfaces;
using System;
using System.ComponentModel;

namespace OntologicalStudio.Desktop.ViewModels;

/// <summary>
/// Bridges the <see cref="IAiOperationStatusService"/> to a bindable view-model
/// for the AI operation overlay. Adds a live "elapsed" counter that ticks every 250ms
/// while an operation is in flight.
/// </summary>
public partial class AiOperationOverlayViewModel : ObservableObject, IDisposable
{
    private readonly IAiOperationStatusService _status;
    private readonly DispatcherTimer _timer;

    public AiOperationOverlayViewModel(IAiOperationStatusService status)
    {
        _status = status;
        _status.PropertyChanged += OnStatusPropertyChanged;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += (_, _) => OnPropertyChanged(nameof(ElapsedText));
    }

    public bool IsBusy => _status.IsBusy;
    public string Title => _status.Title;
    public string Provider => _status.Provider;
    public bool HasProvider => !string.IsNullOrWhiteSpace(_status.Provider);

    public string StatusText => _status.IsCancellationRequested
        ? "Cancelling — waiting for the request to abort…"
        : "Calling the model. This may take a few seconds depending on the model and prompt size.";

    public string ElapsedText
    {
        get
        {
            if (_status.StartedAtUtc is null) return "00:00";
            var elapsed = DateTime.UtcNow - _status.StartedAtUtc.Value;
            return elapsed.TotalHours >= 1
                ? elapsed.ToString(@"h\:mm\:ss")
                : elapsed.ToString(@"mm\:ss");
        }
    }

    public bool CanCancel => _status.IsBusy && !_status.IsCancellationRequested;

    public string CancelButtonLabel => _status.IsCancellationRequested ? "Cancelling…" : "Cancel";

    [RelayCommand]
    private void Cancel()
    {
        _status.Cancel();
    }

    private void OnStatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Forward the most relevant properties. We don't know which one changed in detail
            // so we notify all derived properties — cheap and bulletproof.
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Provider));
            OnPropertyChanged(nameof(HasProvider));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ElapsedText));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(CancelButtonLabel));

            if (_status.IsBusy && !_timer.IsEnabled) _timer.Start();
            if (!_status.IsBusy && _timer.IsEnabled) _timer.Stop();
        });
    }

    public void Dispose()
    {
        _status.PropertyChanged -= OnStatusPropertyChanged;
        _timer.Stop();
    }
}
