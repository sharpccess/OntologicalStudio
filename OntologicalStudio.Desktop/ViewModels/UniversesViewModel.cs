using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class UniversesViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly ILocalizationService _localization;
    private static readonly string StartupLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OntologicalStudio",
        "startup.log");

    public ObservableCollection<Universe> Items { get; } = new();

    [ObservableProperty]
    private Universe? selectedUniverse;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newDescription = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public bool HasSelectedUniverse => SelectedUniverse is not null;

    public event Action? UniversesChanged;
    public event Action? SelectionChanged;

    public UniversesViewModel(IServiceProvider provider)
    {
        _provider = provider;
        _localization = provider.GetRequiredService<ILocalizationService>();
    }

    partial void OnSelectedUniverseChanged(Universe? value)
    {
        OnPropertyChanged(nameof(HasSelectedUniverse));
        if (value is not null)
        {
            NewName = value.Name;
            NewDescription = value.Description;
        }
        SelectionChanged?.Invoke();
    }

    public void NotifyDataChanged() => UniversesChanged?.Invoke();

    public async Task LoadAsync()
    {
        try
        {
            var data = await ScopedRunner.RunAsync<IUniverseService, System.Collections.Generic.IEnumerable<Universe>>(
                _provider, s => s.GetAllAsync());
            var ordered = data.OrderBy(u => u.Name).ToList();
            var prevId = SelectedUniverse?.Id;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Items.Clear();
                foreach (var u in ordered)
                    Items.Add(u);

                SelectedUniverse = prevId.HasValue
                    ? Items.FirstOrDefault(x => x.Id == prevId)
                    : null;

                StatusMessage = _localization.CurrentLanguageCode == "es"
                    ? SelectedUniverse is null
                        ? $"{Items.Count} universo(s) cargado(s). Selecciona uno en la lista para activarlo."
                        : $"{Items.Count} universo(s) cargado(s)."
                    : SelectedUniverse is null
                        ? $"{Items.Count} universe(s) loaded. Select one in the list to activate it."
                        : $"{Items.Count} universe(s) loaded.";
            });
            WriteStartupLog($"UniversesViewModel LoadAsync loaded | count={Items.Count} | names={string.Join(", ", ordered.Select(x => x.Name))}");
            UniversesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task CreateUniverseAsync()
    {
        var name = NewName?.Trim() ?? string.Empty;
        var description = NewDescription?.Trim() ?? string.Empty;

        WriteStartupLog($"UniversesViewModel CreateUniverseAsync start | name='{name}'");

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = _localization.CurrentLanguageCode == "es" ? "El nombre es obligatorio." : "Name is required.";
            WriteStartupLog("UniversesViewModel CreateUniverseAsync aborted | empty name");
            return;
        }

        try
        {
            var created = await ScopedRunner.RunAsync<IUniverseService, Universe>(
                _provider,
                s => s.CreateAsync(name, description));

            WriteStartupLog($"UniversesViewModel CreateUniverseAsync created | id={created.Id}");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                NewName = string.Empty;
                NewDescription = string.Empty;
            });
            await LoadAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = _localization.CurrentLanguageCode == "es"
                    ? $"Universo '{created.Name}' creado. Selecciónalo en la lista para activarlo."
                    : $"Universe '{created.Name}' created. Select it in the list to activate it.";
            });
            WriteStartupLog($"UniversesViewModel CreateUniverseAsync completed | selected={(SelectedUniverse?.Id.ToString() ?? "null")}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
            WriteStartupLog($"UniversesViewModel CreateUniverseAsync error: {ex}");
        }
    }

    public async Task DeleteSelectedUniverseAsync()
    {
        if (SelectedUniverse is null)
            return;

        var id = SelectedUniverse.Id;
        try
        {
            await ScopedRunner.RunAsync<IUniverseService>(_provider, s => s.DeleteAsync(id));
            SelectedUniverse = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
            WriteStartupLog($"UniversesViewModel DeleteSelectedUniverseAsync error: {ex}");
        }
    }

    public async Task RefreshUniversesAsync() => await LoadAsync();

    public async Task UpdateSelectedUniverseAsync()
    {
        if (SelectedUniverse is null)
            return;

        var name = NewName?.Trim() ?? string.Empty;
        var description = NewDescription?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = _localization.CurrentLanguageCode == "es" ? "El nombre es obligatorio." : "Name is required.";
            return;
        }

        try
        {
            SelectedUniverse.Name = name;
            SelectedUniverse.Description = description;
            await ScopedRunner.RunAsync<IUniverseService>(
                _provider,
                service => service.UpdateAsync(SelectedUniverse));
            await LoadAsync();
            SelectedUniverse = Items.FirstOrDefault(x => x.Id == SelectedUniverse?.Id);
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? "Universo actualizado."
                : "Universe updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? $"Falló la actualización: {ex.Message}"
                : $"Update failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateAsync() => await CreateUniverseAsync();

    [RelayCommand]
    private async Task DeleteAsync() => await DeleteSelectedUniverseAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await RefreshUniversesAsync();

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
