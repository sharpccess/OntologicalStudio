using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.ViewModels;

namespace OntologicalStudio.Desktop.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnEditEntityClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel || viewModel.SelectedEntityItem is null)
            return;

        var updated = await ShowLibraryEditDialogAsync(
            viewModel.SelectedEntityItem.Name,
            viewModel.SelectedEntityItem.Description,
            App.Current?.Resources["library.editEntityTitle"]?.ToString() ?? "Edit library entity");

        if (updated is null)
            return;

        viewModel.SelectedEntityItem.Name = updated.Value.name;
        viewModel.SelectedEntityItem.Description = updated.Value.description;
        await viewModel.SaveEntityItemAsync(viewModel.SelectedEntityItem);
    }

    private async void OnEditUniverseModelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel || viewModel.SelectedUniverseModelItem is null)
            return;

        var updated = await ShowLibraryEditDialogAsync(
            viewModel.SelectedUniverseModelItem.Name,
            viewModel.SelectedUniverseModelItem.Description,
            App.Current?.Resources["library.editModelTitle"]?.ToString() ?? "Edit library model");

        if (updated is null)
            return;

        viewModel.SelectedUniverseModelItem.Name = updated.Value.name;
        viewModel.SelectedUniverseModelItem.Description = updated.Value.description;
        await viewModel.SaveUniverseModelItemAsync(viewModel.SelectedUniverseModelItem);
    }

    private async void OnDeleteEntityClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel || viewModel.SelectedEntityItem is null)
            return;

        var confirmed = await ShowConfirmationDialogAsync(
            App.Current?.Resources["library.confirmDeleteEntityTitle"]?.ToString() ?? "Delete library entity",
            string.Format(
                App.Current?.Resources["library.confirmDeleteEntityMessage"]?.ToString() ?? "Delete '{0}' from the library?",
                viewModel.SelectedEntityItem.Name));

        if (!confirmed)
            return;

        await viewModel.DeleteSelectedEntityCommand.ExecuteAsync(null);
    }

    private async void OnDeleteUniverseModelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel || viewModel.SelectedUniverseModelItem is null)
            return;

        var confirmed = await ShowConfirmationDialogAsync(
            App.Current?.Resources["library.confirmDeleteModelTitle"]?.ToString() ?? "Delete library model",
            string.Format(
                App.Current?.Resources["library.confirmDeleteModelMessage"]?.ToString() ?? "Delete '{0}' from the library?",
                viewModel.SelectedUniverseModelItem.Name));

        if (!confirmed)
            return;

        await viewModel.DeleteSelectedUniverseModelCommand.ExecuteAsync(null);
    }

    private async Task<(string name, string description)?> ShowLibraryEditDialogAsync(string name, string description, string title)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return null;

        var nameBox = new TextBox
        {
            Text = name,
            Watermark = App.Current?.Resources["common.name"]?.ToString() ?? "Name"
        };

        var descriptionBox = new TextBox
        {
            Text = description,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Height = 140,
            Watermark = App.Current?.Resources["common.description"]?.ToString() ?? "Description"
        };

        (string name, string description)? result = null;

        var dialog = new Window
        {
            Title = title,
            Width = 520,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.Full
        };

        var cancelButton = new Button
        {
            Content = App.Current?.Resources["cancel"]?.ToString() ?? "Cancel",
            MinWidth = 90
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var saveButton = new Button
        {
            Content = App.Current?.Resources["common.update"]?.ToString() ?? "Update",
            MinWidth = 90
        };
        saveButton.Click += (_, _) =>
        {
            result = ((nameBox.Text ?? string.Empty).Trim(), (descriptionBox.Text ?? string.Empty).Trim());
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                nameBox,
                descriptionBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelButton,
                        saveButton
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
        owner.Activate();
        owner.Focus();
        return result;
    }

    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return false;

        var confirmed = false;
        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.Full
        };

        var cancelButton = new Button
        {
            Content = App.Current?.Resources["cancel"]?.ToString() ?? "Cancel",
            MinWidth = 90
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var confirmButton = new Button
        {
            Content = App.Current?.Resources["confirm"]?.ToString() ?? "Confirm",
            MinWidth = 90
        };
        confirmButton.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancelButton,
                        confirmButton
                    }
                }
            }
        };

        await dialog.ShowDialog(owner);
        owner.Activate();
        owner.Focus();
        return confirmed;
    }
}