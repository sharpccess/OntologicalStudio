using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.ViewModels;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace OntologicalStudio.Desktop.Views;

public partial class ScenariosView : UserControl
{
    public ScenariosView() { AvaloniaXamlLoader.Load(this); }

    private async void OnCopyPromptClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not SolutionsViewModel viewModel)
            return;

        var prompt = viewModel.SelectedSolution?.PromptSnapshot;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            viewModel.StatusMessage = "No prompt available to copy.";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            viewModel.StatusMessage = "Clipboard is not available.";
            return;
        }

        await topLevel.Clipboard.SetTextAsync(prompt);
        viewModel.StatusMessage = "Prompt copied to clipboard.";
    }

    private async void OnSavePromptClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not SolutionsViewModel viewModel)
            return;

        var prompt = viewModel.SelectedSolution?.PromptSnapshot;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            viewModel.StatusMessage = "No prompt available to save.";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            viewModel.StatusMessage = "Save dialog is not available.";
            return;
        }

        var suggestedName = $"{BuildSafeFileName(viewModel.SelectedSolution?.Title ?? "prompt")}-prompt.txt";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save prompt",
            SuggestedFileName = suggestedName,
            DefaultExtension = ".txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Prompt text")
                {
                    Patterns = new[] { "*.txt" },
                    MimeTypes = new[] { "text/plain" }
                }
            }
        });

        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        var bytes = Encoding.UTF8.GetBytes(prompt);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
        viewModel.StatusMessage = $"Prompt saved to: {file.Name}";
    }

    private async void OnExportTxtClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Text);

    private async void OnExportMarkdownClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Markdown);

    private async void OnExportPdfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Pdf);

    private async void OnExportWordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Word);

    private async void OnCopyFormattedArtifactClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not SolutionsViewModel viewModel)
            return;

        var html = viewModel.BuildSelectedArtifactHtml();
        if (string.IsNullOrWhiteSpace(html))
        {
            viewModel.StatusMessage = "No formatted artifact available to copy.";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            viewModel.StatusMessage = "Clipboard is not available.";
            return;
        }

        var plainText = WebUtility.HtmlDecode(StripHtml(html));
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, plainText);
        dataObject.Set("text/html", html);
        await topLevel.Clipboard.SetDataObjectAsync(dataObject);
        viewModel.StatusMessage = "Formatted artifact copied to clipboard.";
    }

    private async Task ExportAsync(object? sender, ArtifactExportFormat format)
    {
        if ((sender as Control)?.DataContext is not SolutionsViewModel viewModel)
            return;

        var payload = await viewModel.BuildExportAsync(format);
        if (payload is null || payload.Content.Length == 0)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            viewModel.StatusMessage = "Save dialog is not available.";
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export artifact",
            SuggestedFileName = payload.FileName,
            DefaultExtension = Path.GetExtension(payload.FileName),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Export")
                {
                    Patterns = new[] { $"*{Path.GetExtension(payload.FileName)}" },
                    MimeTypes = new[] { payload.MimeType }
                }
            }
        });

        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(payload.Content);
        await stream.FlushAsync();
        viewModel.StatusMessage = $"Exported to: {file.Name}";

        try
        {
            var localPath = file.TryGetLocalPath();
            var directoryPath = string.IsNullOrWhiteSpace(localPath)
                ? null
                : Path.GetDirectoryName(localPath);

            if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = directoryPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Exported to: {file.Name}. Folder open failed: {ex.Message}";
        }
    }

    private static string BuildSafeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(input.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "prompt" : cleaned;
    }

    private static string StripHtml(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        return text.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
    }
}
