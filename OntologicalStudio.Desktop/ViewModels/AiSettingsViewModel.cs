using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Localization.Services;

namespace OntologicalStudio.Desktop.ViewModels;

public partial class AiSettingsViewModel : ObservableObject
{
    private readonly IAiConnectionSettingsService _settingsService;
    private readonly ILocalizationService _localization;

    public IReadOnlyList<AiProviderOption> Providers { get; } = new[]
    {
        new AiProviderOption("ollama", "Ollama"),
        new AiProviderOption("openrouter", "OpenRouter"),
        new AiProviderOption("openai", "OpenAI / GPT"),
        new AiProviderOption("anthropic", "Anthropic / Claude")
    };

    [ObservableProperty]
    private AiProviderOption? selectedProvider;

    [ObservableProperty]
    private string ollamaEndpoint = string.Empty;

    [ObservableProperty]
    private string ollamaApiKey = string.Empty;

    [ObservableProperty]
    private string ollamaModel = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public AiSettingsViewModel(IServiceProvider provider)
    {
        _settingsService = provider.GetRequiredService<IAiConnectionSettingsService>();
        _localization = provider.GetRequiredService<ILocalizationService>();
    }

    public async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync();
        OllamaEndpoint = settings.ApiEndpoint;
        OllamaApiKey = settings.ApiKey;
        OllamaModel = settings.Model;
        SelectedProvider = Providers.FirstOrDefault(x => x.Key == settings.Provider) ?? Providers.First();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _settingsService.SaveAsync(new AiConnectionSettings
            {
                Provider = SelectedProvider?.Key ?? "ollama",
                ApiEndpoint = OllamaEndpoint,
                ApiKey = OllamaApiKey,
                Model = OllamaModel,
                OllamaEndpoint = OllamaEndpoint,
                OllamaApiKey = OllamaApiKey,
                OllamaModel = OllamaModel
            });

            StatusMessage = _localization.T("ai.settings.saved");
        }
        catch (Exception ex)
        {
            StatusMessage = $"{_localization.T("notifications.error")}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (IsBusy)
            return;

        OllamaEndpoint = string.Empty;
        OllamaApiKey = string.Empty;
        OllamaModel = string.Empty;
        SelectedProvider = Providers.FirstOrDefault(x => x.Key == "ollama") ?? Providers.First();
        await SaveAsync();
        StatusMessage = _localization.T("ai.settings.cleared");
    }
}

public class AiProviderOption
{
    public AiProviderOption(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }

    public string Key { get; }
    public string DisplayName { get; }
}