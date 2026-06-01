using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using OntologicalStudio.Core.Interfaces;

namespace OntologicalStudio.Infrastructure.Services;

/// <summary>
/// Thread-safe singleton implementation of <see cref="IAiOperationStatusService"/>.
/// </summary>
public sealed class AiOperationStatusService : IAiOperationStatusService
{
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string _title = string.Empty;
    private string _provider = string.Empty;
    private DateTime? _startedAtUtc;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string Title
    {
        get => _title;
        private set => SetField(ref _title, value ?? string.Empty);
    }

    public string Provider
    {
        get => _provider;
        private set => SetField(ref _provider, value ?? string.Empty);
    }

    public DateTime? StartedAtUtc
    {
        get => _startedAtUtc;
        private set => SetField(ref _startedAtUtc, value);
    }

    public CancellationToken Token
    {
        get
        {
            lock (_lock)
            {
                return _cts?.Token ?? CancellationToken.None;
            }
        }
    }

    public bool IsCancellationRequested
    {
        get
        {
            lock (_lock)
            {
                return _cts?.IsCancellationRequested ?? false;
            }
        }
    }

    public CancellationToken Begin(string title, string provider)
    {
        CancellationToken token;
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }
        Title = title ?? string.Empty;
        Provider = provider ?? string.Empty;
        StartedAtUtc = DateTime.UtcNow;
        IsBusy = true;
        return token;
    }

    public void UpdateTitle(string title) => Title = title;

    public void UpdateProvider(string provider) => Provider = provider;

    public void Cancel()
    {
        lock (_lock)
        {
            try { _cts?.Cancel(); }
            catch { /* already disposed */ }
        }
        // Notify so the UI updates Cancel button state, but don't end the op yet —
        // the running task will catch OperationCanceledException and call End().
        OnPropertyChanged(nameof(IsCancellationRequested));
    }

    public void End()
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
        }
        IsBusy = false;
        StartedAtUtc = null;
        Title = string.Empty;
        Provider = string.Empty;
        OnPropertyChanged(nameof(IsCancellationRequested));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(property);
    }

    private void OnPropertyChanged(string? property)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
