using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Desktop.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OntologicalStudio.Desktop.Views;

public partial class UniversesView : UserControl
{
    private readonly TextBox? _nameTextBox;
    private readonly TextBox? _descriptionTextBox;
    private readonly StackPanel? _universeItemsPanel;
    private readonly TextBlock? _universeListStatusText;
    private readonly TextBlock? _universeEditorStatusText;
    private readonly TextBlock? _selectedUniverseIdText;
    private readonly TextBlock? _selectedUniverseCreatedText;
    private readonly TextBlock? _selectedUniverseNameText;
    private UniversesViewModel? _viewModel;

    public UniversesView()
    {
        AvaloniaXamlLoader.Load(this);
        _nameTextBox = this.FindControl<TextBox>("UniverseNameTextBox");
        _descriptionTextBox = this.FindControl<TextBox>("UniverseDescriptionTextBox");
        _universeItemsPanel = this.FindControl<StackPanel>("UniverseItemsPanel");
        _universeListStatusText = this.FindControl<TextBlock>("UniverseListStatusText");
        _universeEditorStatusText = this.FindControl<TextBlock>("UniverseEditorStatusText");
        _selectedUniverseIdText = this.FindControl<TextBlock>("SelectedUniverseIdText");
        _selectedUniverseCreatedText = this.FindControl<TextBlock>("SelectedUniverseCreatedText");
        _selectedUniverseNameText = this.FindControl<TextBlock>("SelectedUniverseNameText");

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Items.CollectionChanged -= OnItemsCollectionChanged;
        }

        _viewModel = DataContext as UniversesViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Items.CollectionChanged += OnItemsCollectionChanged;
        }

        RefreshUniverseView();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshUniverseView();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UniversesViewModel.StatusMessage) or nameof(UniversesViewModel.SelectedUniverse))
            RefreshUniverseView();
    }

    private void RefreshUniverseView()
    {
        if (_viewModel is null)
            return;

        if (_universeListStatusText is not null)
            _universeListStatusText.Text = _viewModel.StatusMessage;
        if (_universeEditorStatusText is not null)
            _universeEditorStatusText.Text = _viewModel.StatusMessage;

        var selectedUniverse = _viewModel.SelectedUniverse;
        if (_selectedUniverseIdText is not null)
            _selectedUniverseIdText.Text = selectedUniverse?.Id.ToString() ?? string.Empty;
        if (_selectedUniverseCreatedText is not null)
            _selectedUniverseCreatedText.Text = selectedUniverse?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        if (_selectedUniverseNameText is not null)
            _selectedUniverseNameText.Text = selectedUniverse?.Name ?? string.Empty;

        RebuildUniverseCards();
    }

    private void RebuildUniverseCards()
    {
        if (_viewModel is null || _universeItemsPanel is null)
            return;

        _universeItemsPanel.Children.Clear();

        if (_viewModel.Items.Count == 0)
        {
            _universeItemsPanel.Children.Add(new TextBlock
            {
                Text = "No universes yet.",
                Opacity = 0.7,
                Margin = new Thickness(4)
            });
            return;
        }

        foreach (var universe in _viewModel.Items)
        {
            var isSelected = _viewModel.SelectedUniverse?.Id == universe.Id;

            var button = new Button
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(Color.Parse(isSelected ? "#2b5f86" : "#20262e")),
                BorderBrush = new SolidColorBrush(Color.Parse(isSelected ? "#7fd1ff" : "#3e4a56")),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Content = new Border
                {
                    Padding = new Thickness(8),
                    Background = Brushes.Transparent,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = universe.Name,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = Brushes.White
                            },
                            new TextBlock
                            {
                                Text = string.IsNullOrWhiteSpace(universe.Description) ? "(no description)" : universe.Description,
                                Foreground = new SolidColorBrush(Color.Parse("#c5d0db")),
                                Opacity = 0.85,
                                FontSize = 11,
                                TextWrapping = TextWrapping.Wrap,
                                MaxHeight = 34
                            }
                        }
                    }
                }
            };

            button.Click += (_, _) =>
            {
                if (_viewModel.SelectedUniverse?.Id == universe.Id)
                    _viewModel.SelectedUniverse = null;
                else
                    _viewModel.SelectedUniverse = universe;

                RefreshUniverseView();
            };

            _universeItemsPanel.Children.Add(button);
        }
    }

    private async void OnCreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
        {
            vm.NewName = _nameTextBox?.Text ?? string.Empty;
            vm.NewDescription = _descriptionTextBox?.Text ?? string.Empty;
            await vm.CreateUniverseAsync();
        }
    }

    private async void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
            await vm.DeleteSelectedUniverseAsync();
    }

    private async void OnUpdateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
        {
            vm.NewName = _nameTextBox?.Text ?? string.Empty;
            vm.NewDescription = _descriptionTextBox?.Text ?? string.Empty;
            await vm.UpdateSelectedUniverseAsync();
        }
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
            await vm.RefreshUniversesAsync();
    }
}
