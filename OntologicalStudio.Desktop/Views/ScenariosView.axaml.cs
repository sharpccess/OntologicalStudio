using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.ViewModels;
using System.Diagnostics;

namespace OntologicalStudio.Desktop.Views;

public partial class ScenariosView : UserControl
{
    public ScenariosView() { AvaloniaXamlLoader.Load(this); }

    private async void OnExportTxtClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Text);

    private async void OnExportMarkdownClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Markdown);

    private async void OnExportPdfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Pdf);

    private async void OnExportWordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await ExportAsync(sender, ArtifactExportFormat.Word);

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
}
