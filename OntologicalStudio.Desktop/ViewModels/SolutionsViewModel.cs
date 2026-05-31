using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class SolutionsViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly ILocalizationService _localization;

    public ObservableCollection<Solution> Items { get; } = new();
    public ObservableCollection<SolutionArtifactViewModel> SelectedSolutionArtifacts { get; } = new();
    public ObservableCollection<string> ResolutionStyles { get; } = new();

    [ObservableProperty]
    private Scenario? currentScenario;

    [ObservableProperty]
    private Solution? selectedSolution;

    [ObservableProperty]
    private string extraInstructions = string.Empty;

    [ObservableProperty]
    private string selectedResolutionStyle = string.Empty;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string statusMessage = "Select a scenario to view its solutions.";

    public SolutionsViewModel(IServiceProvider provider)
    {
        _provider = provider;
        _localization = provider.GetRequiredService<ILocalizationService>();
        _localization.OnLanguageChanged += HandleLanguageChanged;
        RebuildResolutionStyles();
    }

    partial void OnCurrentScenarioChanged(Scenario? value) => _ = LoadAsync();

    partial void OnSelectedSolutionChanged(Solution? value)
    {
        RebuildSelectedArtifacts();
    }

    partial void OnSelectedResolutionStyleChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var instruction = value.Trim();
        if (string.IsNullOrWhiteSpace(ExtraInstructions))
            ExtraInstructions = instruction;
        else if (!ExtraInstructions.Contains(instruction, StringComparison.OrdinalIgnoreCase))
            ExtraInstructions = $"{ExtraInstructions.Trim()}{Environment.NewLine}{instruction}";
    }

    private void HandleLanguageChanged()
    {
        RebuildResolutionStyles();
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        Items.Clear();
        SelectedSolution = null;
        if (CurrentScenario is null)
        {
            StatusMessage = _localization.T("scenarios.selectScenarioToViewSolutions");
            return;
        }
        try
        {
            var data = await ScopedRunner.RunAsync<ISolutionService, IEnumerable<Solution>>(
                _provider, s => s.GetByScenarioAsync(CurrentScenario.Id));
            foreach (var s in data)
                Items.Add(LocalizeSolution(s));
            SelectedSolution = Items.FirstOrDefault();
            StatusMessage = $"{Items.Count} solution(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (CurrentScenario is null) { StatusMessage = _localization.T("scenarios.selectScenarioFirst"); return; }
        if (IsRunning) return;
        IsRunning = true;
        StatusMessage = _localization.T("scenarios.running");
        try
        {
            var sol = await ScopedRunner.RunAsync<ISolutionService, Solution>(
                _provider,
                s => s.RunAsync(
                    CurrentScenario.Id,
                    string.IsNullOrWhiteSpace(ExtraInstructions) ? null : ExtraInstructions,
                    _localization.CurrentLanguageCode));
            ExtraInstructions = string.Empty;
            await LoadAsync();
            SelectedSolution = Items.FirstOrDefault(x => x.Id == sol.Id);
            StatusMessage = _localization.T("scenarios.solutionCreated", sol.Artifacts.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSolution is null) return;
        var id = SelectedSolution.Id;
        try
        {
            await ScopedRunner.RunAsync<ISolutionService>(_provider, s => s.DeleteAsync(id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MarkFinalAsync()
    {
        if (SelectedSolution is null) return;
        var id = SelectedSolution.Id;
        try
        {
            await ScopedRunner.RunAsync<ISolutionService>(_provider, s => s.UpdateStatusAsync(id, SolutionStatus.Final));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update failed: {ex.Message}";
        }
    }

    private Solution LocalizeSolution(Solution solution)
    {
        foreach (var artifact in solution.Artifacts)
        {
            artifact.Label = LocalizeArtifactLabel(artifact.Label);
        }

        return solution;
    }

    private void RebuildSelectedArtifacts()
    {
        SelectedSolutionArtifacts.Clear();
        if (SelectedSolution is null)
            return;

        foreach (var artifact in SelectedSolution.Artifacts.OrderBy(x => x.Order))
        {
            SelectedSolutionArtifacts.Add(new SolutionArtifactViewModel
            {
                Label = LocalizeArtifactLabel(artifact.Label),
                KindDisplay = LocalizeArtifactKind(artifact.Kind),
                MimeType = artifact.MimeType,
                InlineContent = artifact.InlineContent
            });
        }
    }

    private string LocalizeArtifactKind(ArtifactKind kind)
    {
        return kind switch
        {
            ArtifactKind.Text => _localization.T("artifact.kind.text"),
            ArtifactKind.Markdown => _localization.T("artifact.kind.markdown"),
            ArtifactKind.Json => _localization.T("artifact.kind.json"),
            ArtifactKind.Image => _localization.T("artifact.kind.image"),
            ArtifactKind.File => _localization.T("artifact.kind.file"),
            _ => kind.ToString()
        };
    }

    private string LocalizeArtifactLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label;

        return label switch
        {
            "AI response" => _localization.CurrentLanguageCode == "es" ? "Respuesta de IA" : "AI response",
            "Respuesta de IA" => _localization.CurrentLanguageCode == "es" ? "Respuesta de IA" : "AI response",
            _ => label
        };
    }

    private void RebuildResolutionStyles()
    {
        ResolutionStyles.Clear();
        if (_localization.CurrentLanguageCode == "es")
        {
            ResolutionStyles.Add("Diagnóstico estratégico");
            ResolutionStyles.Add("Plan de acción paso a paso");
            ResolutionStyles.Add("Análisis de riesgos y mitigaciones");
            ResolutionStyles.Add("Opciones comparadas con pros y contras");
            ResolutionStyles.Add("Respuesta breve y ejecutiva");
        }
        else
        {
            ResolutionStyles.Add("Strategic diagnosis");
            ResolutionStyles.Add("Step-by-step action plan");
            ResolutionStyles.Add("Risk analysis and mitigations");
            ResolutionStyles.Add("Compared options with pros and cons");
            ResolutionStyles.Add("Short executive response");
        }

        SelectedResolutionStyle = string.Empty;
    }
}

public class SolutionArtifactViewModel
{
    public string Label { get; set; } = string.Empty;
    public string KindDisplay { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string InlineContent { get; set; } = string.Empty;
}
