using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class PromptPreviewViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly UniversesViewModel _universes;

    public ObservableCollection<Scenario> Scenarios { get; } = new();

    [ObservableProperty]
    private Scenario? selectedScenario;

    [ObservableProperty]
    private string promptText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Select a universe and a scenario, then click Generate.";

    public PromptPreviewViewModel(IServiceProvider provider, UniversesViewModel universes)
    {
        _provider = provider;
        _universes = universes;
        _universes.SelectionChanged += async () => await ReloadScenariosAsync();
        _universes.UniversesChanged += async () => await ReloadScenariosAsync();
    }

    partial void OnSelectedScenarioChanged(Scenario? value)
    {
        if (value is not null)
            _ = GenerateAsync();
    }

    public async Task ReloadScenariosAsync()
    {
        Scenarios.Clear();
        PromptText = string.Empty;
        var u = _universes.SelectedUniverse;
        if (u is null) return;
        try
        {
            var data = await ScopedRunner.RunAsync<IScenarioService, IEnumerable<Scenario>>(
                _provider, s => s.GetByUniverseAsync(u.Id));
            foreach (var s in data.OrderByDescending(s => s.CreatedAt))
                Scenarios.Add(s);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        var u = _universes.SelectedUniverse;
        if (u is null) { StatusMessage = "Select a universe first."; return; }
        if (SelectedScenario is null) { StatusMessage = "Select a scenario."; return; }

        try
        {
            var entities = (await ScopedRunner.RunAsync<IEntityService, IEnumerable<Entity>>(
                _provider, s => s.GetByUniverseAsync(u.Id))).ToList();

            var seen = new HashSet<Guid>();
            var rels = new List<Relationship>();
            foreach (var e in entities)
            {
                var rs = await ScopedRunner.RunAsync<IRelationshipRepository, IEnumerable<Relationship>>(
                    _provider, r => r.GetBySourceEntityAsync(e.Id));
                foreach (var r in rs)
                    if (seen.Add(r.Id)) rels.Add(r);
            }

            PromptText = BuildPrompt(u, SelectedScenario, entities, rels);
            StatusMessage = $"Prompt generated ({PromptText.Length} chars).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private static string BuildPrompt(Universe universe, Scenario scenario, List<Entity> entities, List<Relationship> relationships)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### CONTEXT MODEL FOR REASONING");
        sb.AppendLine($"**Universe:** {universe.Name}");
        if (!string.IsNullOrWhiteSpace(universe.Description))
            sb.AppendLine($"**Universe Description:** {universe.Description}");
        sb.AppendLine();
        sb.AppendLine($"**Problem Title:** {scenario.Title}");
        sb.AppendLine($"**Situation Description:** {scenario.Description}");
        if (!string.IsNullOrWhiteSpace(scenario.Goals))
            sb.AppendLine($"**Objectives & Constraints:** {scenario.Goals}");
        sb.AppendLine();

        sb.AppendLine("### KEY ENTITIES");
        if (entities.Count == 0)
            sb.AppendLine("_(no entities defined yet)_");
        foreach (var e in entities)
        {
            sb.AppendLine($"- **{e.Name}** ({e.EntityType?.Name ?? "Entity"})");
            if (!string.IsNullOrWhiteSpace(e.Description))
                sb.AppendLine($"  Description: {e.Description}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
            sb.AppendLine($"  Confidence: {e.ConfidenceLevel}% | Completeness: {e.CompletenessScore}%");
        }
        sb.AppendLine();

        sb.AppendLine("### RELATIONSHIPS");
        if (relationships.Count == 0)
            sb.AppendLine("_(no relationships defined yet)_");
        foreach (var r in relationships)
        {
            var src = entities.FirstOrDefault(x => x.Id == r.SourceEntityId)?.Name ?? "?";
            var tgt = entities.FirstOrDefault(x => x.Id == r.TargetEntityId)?.Name ?? "?";
            var type = r.RelationshipType?.Name ?? "relatesTo";
            sb.AppendLine($"- {src} --({type})--> {tgt}");
            if (!string.IsNullOrWhiteSpace(r.Description))
                sb.AppendLine($"  {r.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("### REASONING INSTRUCTIONS");
        sb.AppendLine("Act as a strategic systems analyst. Using the model above:");
        sb.AppendLine("1. Summarize the core situation.");
        sb.AppendLine("2. Identify systemic dynamics and feedback loops between entities.");
        sb.AppendLine("3. Surface contradictions, blind spots, and hidden incentives.");
        sb.AppendLine("4. Assess risks if no intervention is made.");
        sb.AppendLine("5. Propose 3-5 high-leverage interventions, ranked by impact.");
        return sb.ToString();
    }
}
