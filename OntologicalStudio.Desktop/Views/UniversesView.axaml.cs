using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace OntologicalStudio.Desktop.Views;

public partial class UniversesView : UserControl
{
    public UniversesView() { AvaloniaXamlLoader.Load(this); }

    private async void OnCreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
            await ExecuteIfPossibleAsync(vm.CreateCommand);
    }

    private async void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
            await ExecuteIfPossibleAsync(vm.DeleteCommand);
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.UniversesViewModel vm)
            await ExecuteIfPossibleAsync(vm.RefreshCommand);
    }

    private static async Task ExecuteIfPossibleAsync(System.Windows.Input.ICommand command)
    {
        switch (command)
        {
            case CommunityToolkit.Mvvm.Input.IAsyncRelayCommand asyncRelayCommand:
                await asyncRelayCommand.ExecuteAsync(null);
                break;
            default:
                command.Execute(null);
                break;
        }
    }
}
