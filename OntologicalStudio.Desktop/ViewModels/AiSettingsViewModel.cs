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
        new AiProviderOption("anthropic", "Anthropic / Claude"),
        new AiProviderOption("vscode", "VSCode / TRAE Bridge")
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

    /// <summary>True when the currently selected provider does not need an API key (e.g. local bridges).</summary>
    public bool IsApiKeyRequired => SelectedProvider?.Key != "vscode";

    /// <summary>Hint text shown next to the endpoint field, varies per provider.</summary>
    public string EndpointHint => SelectedProvider?.Key switch
    {
        "vscode" => "http://localhost:39217  (VSCode/TRAE bridge)",
        "openrouter" => "https://openrouter.ai/api/v1/chat/completions",
        "openai" => "https://api.openai.com/v1/chat/completions",
        "anthropic" => "https://api.anthropic.com/v1/messages",
        _ => "http://localhost:11434"
    };

    /// <summary>Hint for the api key field, becomes a clear "not needed" message when the bridge is selected.</summary>
    public string ApiKeyHint => SelectedProvider?.Key == "vscode"
        ? "Not required — handled by VSCode/TRAE"
        : string.Empty;

    partial void OnSelectedProviderChanged(AiProviderOption? value)
    {
        OnPropertyChanged(nameof(IsApiKeyRequired));
        OnPropertyChanged(nameof(EndpointHint));
        OnPropertyChanged(nameof(ApiKeyHint));

        // Pre-fill sensible defaults when switching to a provider that has them
        // and the user has not entered anything custom yet.
        if (value?.Key == "vscode" && string.IsNullOrWhiteSpace(OllamaEndpoint))
            OllamaEndpoint = "http://localhost:39217";
    }

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