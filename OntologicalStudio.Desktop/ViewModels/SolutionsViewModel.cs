using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.Services;
using OntologicalStudio.Localization.Services;
using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class SolutionsViewModel : ObservableObject
{
    private readonly IServiceProvider _provider;
    private readonly ILocalizationService _localization;

    public ObservableCollection<Solution> Items { get; } = new();
    public ObservableCollection<SolutionArtifactViewModel> SelectedSolutionArtifacts { get; } = new();
    public ObservableCollection<MarkdownPreviewViewModel> SelectedArtifactMarkdownBlocks { get; } = new();
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

        var statusService = _provider.GetService(typeof(OntologicalStudio.Core.Interfaces.IAiOperationStatusService))
            as OntologicalStudio.Core.Interfaces.IAiOperationStatusService;
        var aiSettingsSvc = _provider.GetService(typeof(OntologicalStudio.Core.Interfaces.IAiConnectionSettingsService))
            as OntologicalStudio.Core.Interfaces.IAiConnectionSettingsService;
        var providerLabel = "AI";
        if (aiSettingsSvc is not null)
        {
            var s = await aiSettingsSvc.GetAsync();
            providerLabel = $"{s.Provider} ({s.Model})";
        }
        var title = _localization.CurrentLanguageCode == "es"
            ? $"Resolviendo escenario '{CurrentScenario.Title}'…"
            : $"Solving scenario '{CurrentScenario.Title}'…";
        statusService?.Begin(title, providerLabel);

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
        catch (OperationCanceledException)
        {
            StatusMessage = _localization.CurrentLanguageCode == "es"
                ? "Resolución cancelada por el usuario."
                : "Run cancelled by the user.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            statusService?.End();
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

    public async Task<ArtifactExportPayload?> BuildExportAsync(ArtifactExportFormat format)
    {
        if (SelectedSolution is null)
        {
            StatusMessage = "Select a solution first.";
            return null;
        }

        try
        {
            var payload = await ScopedRunner.RunAsync<IArtifactExportService, ArtifactExportPayload>(
                _provider,
                service => service.ExportSolutionAsync(SelectedSolution, format));
            return payload;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            return null;
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
        SelectedArtifactMarkdownBlocks.Clear();
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

        var markdownArtifact = SelectedSolution.Artifacts
            .OrderBy(x => x.Order)
            .FirstOrDefault(x =>
                x.Kind == ArtifactKind.Markdown
                || string.Equals(x.MimeType, "text/markdown", StringComparison.OrdinalIgnoreCase));

        var previewSource = markdownArtifact?.InlineContent
            ?? SelectedSolution.Artifacts.OrderBy(x => x.Order).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.InlineContent))?.InlineContent
            ?? string.Empty;

        foreach (var block in BuildMarkdownPreview(previewSource))
            SelectedArtifactMarkdownBlocks.Add(block);
    }

    public string BuildSelectedArtifactHtml()
    {
        if (SelectedArtifactMarkdownBlocks.Count == 0)
            return "<html><body></body></html>";

        var html = new StringBuilder();
        html.Append("<html><body style=\"font-family:Segoe UI, Arial, sans-serif; color:#1f2937;\">");

        foreach (var block in SelectedArtifactMarkdownBlocks)
        {
            var text = WebUtility.HtmlEncode(block.Text);

            if (block.IsCode)
            {
                html.Append("<pre style=\"background:#f3f4f6; padding:8px; border:1px solid #d1d5db; border-radius:4px; font-family:Consolas, monospace; font-size:10pt; white-space:pre-wrap;\">");
                html.Append(text);
                html.Append("</pre>");
                continue;
            }

            if (block.FontSize >= 19)
                html.Append($"<h1>{text}</h1>");
            else if (block.FontSize >= 15)
                html.Append($"<h2>{text}</h2>");
            else if (block.FontSize >= 13 && block.FontWeight == FontWeight.Bold)
                html.Append($"<h3>{text}</h3>");
            else if (text.StartsWith("• "))
                html.Append($"<p style=\"margin-left:12px;\">{text}</p>");
            else
                html.Append($"<p>{text}</p>");
        }

        html.Append("</body></html>");
        return html.ToString();
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
            ResolutionStyles.Add("Usa un estilo de diagnóstico estratégico, totalmente basado en las entidades y relaciones del modelo.");
            ResolutionStyles.Add("Devuelve un plan de acción paso a paso, concreto y accionable, basado en el escenario.");
            ResolutionStyles.Add("Haz un análisis de riesgos y mitigaciones, usando solo el contexto modelado.");
            ResolutionStyles.Add("Compara opciones con pros y contras, conectando cada opción con las entidades implicadas.");
            ResolutionStyles.Add("Responde en formato ejecutivo breve, priorizando claridad y relevancia.");
        }
        else
        {
            ResolutionStyles.Add("Use a strategic diagnosis style fully grounded in the modeled entities and relationships.");
            ResolutionStyles.Add("Return a concrete step-by-step action plan based on the scenario.");
            ResolutionStyles.Add("Provide a risks and mitigations analysis using only the modeled context.");
            ResolutionStyles.Add("Compare options with pros and cons, tying each option to the involved entities.");
            ResolutionStyles.Add("Respond in a short executive format, prioritizing clarity and relevance.");
        }

        SelectedResolutionStyle = string.Empty;
    }

    private static IReadOnlyList<MarkdownPreviewViewModel> BuildMarkdownPreview(string markdown)
    {
        var result = new List<MarkdownPreviewViewModel>();
        if (string.IsNullOrWhiteSpace(markdown))
            return result;

        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
                return;

            result.Add(new MarkdownPreviewViewModel
            {
                Text = string.Join(" ", paragraphLines).Trim(),
                FontSize = 13,
                FontWeight = FontWeight.Normal,
                Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                Margin = new Thickness(0, 0, 0, 10)
            });
            paragraphLines.Clear();
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                if (inCodeBlock)
                {
                    result.Add(new MarkdownPreviewViewModel
                    {
                        Text = string.Join(Environment.NewLine, codeLines),
                        FontSize = 11,
                        FontWeight = FontWeight.Normal,
                        IsCode = true,
                        Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                        Background = new SolidColorBrush(Color.Parse("#111827")),
                        BorderBrush = new SolidColorBrush(Color.Parse("#374151")),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8),
                        FontFamily = new FontFamily("Consolas"),
                        Margin = new Thickness(0, 0, 0, 10)
                    });
                    codeLines.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushParagraph();
                result.Add(new MarkdownPreviewViewModel
                {
                    Text = line[2..].Trim(),
                    FontSize = 24,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#F9FAFB")),
                    Margin = new Thickness(0, 6, 0, 14)
                });
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushParagraph();
                result.Add(new MarkdownPreviewViewModel
                {
                    Text = line[3..].Trim(),
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#F3F4F6")),
                    Margin = new Thickness(0, 10, 0, 8)
                });
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushParagraph();
                result.Add(new MarkdownPreviewViewModel
                {
                    Text = line[4..].Trim(),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                    Margin = new Thickness(0, 6, 0, 6)
                });
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph();
                result.Add(new MarkdownPreviewViewModel
                {
                    Text = $"•  {line[2..].Trim()}",
                    FontSize = 13,
                    FontWeight = FontWeight.Normal,
                    Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                    Margin = new Thickness(12, 0, 0, 6)
                });
                continue;
            }

            paragraphLines.Add(StripInlineMarkdown(line));
        }

        FlushParagraph();
        return result;
    }

    private static string StripInlineMarkdown(string line)
    {
        var text = line;
        text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "$1");
        text = text.Replace("**", string.Empty, StringComparison.Ordinal)
                   .Replace("__", string.Empty, StringComparison.Ordinal)
                   .Replace("`", string.Empty, StringComparison.Ordinal)
                   .Replace("_", string.Empty, StringComparison.Ordinal);
        return text.Trim();
    }
}

public class SolutionArtifactViewModel
{
    public string Label { get; set; } = string.Empty;
    public string KindDisplay { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string InlineContent { get; set; } = string.Empty;
}
