using System;
using System.ComponentModel;
using System.Threading;

namespace OntologicalStudio.Core.Interfaces;

/// <summary>
/// Tracks the state of the current AI operation (hydration, scenario solve, etc.)
/// so the UI can show a global "in progress" overlay with elapsed time and a Cancel button.
/// </summary>
public interface IAiOperationStatusService : INotifyPropertyChanged
{
    bool IsBusy { get; }

    /// <summary>Short text describing what the call is about (e.g. "Hydrating 'Carlos'").</summary>
    string Title { get; }

    /// <summary>Optional secondary text (e.g. provider name "DeepSeek (deepseek-chat)").</summary>
    string Provider { get; }

    /// <summary>UTC time when the current operation started, or null if idle.</summary>
    DateTime? StartedAtUtc { get; }

    /// <summary>Cancellation token to pass down to HttpClient.SendAsync / provider calls.</summary>
    CancellationToken Token { get; }

    /// <summary>True if the user has requested a cancel.</summary>
    bool IsCancellationRequested { get; }

    /// <summary>
    /// Starts a new operation. Returns the cancellation token to propagate.
    /// Resets any previous state.
    /// </summary>
    CancellationToken Begin(string title, string provider);

    /// <summary>Updates the title without restarting the elapsed timer.</summary>
    void UpdateTitle(string title);

    /// <summary>Updates the provider label without restarting the elapsed timer.</summary>
    void UpdateProvider(string provider);

    /// <summary>Cancels the current operation (signals the token).</summary>
    void Cancel();

    /// <summary>Marks the operation as finished. Safe to call multiple times.</summary>
    void End();
}
