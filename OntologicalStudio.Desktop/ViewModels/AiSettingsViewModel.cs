using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Localization.Services;
using System.Net.Http;
using System.Text.Json;

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
        new AiProviderOption("deepseek", "DeepSeek"),
        new AiProviderOption("gemini", "Google Gemini")
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

    /// <summary>Hint text shown next to the endpoint field, varies per provider.</summary>
    public string EndpointHint => SelectedProvider?.Key switch
    {
        "openrouter" => "https://openrouter.ai/api/v1/chat/completions",
        "openai" => "https://api.openai.com/v1/chat/completions",
        "anthropic" => "https://api.anthropic.com/v1/messages",
        "deepseek" => "https://api.deepseek.com/v1/chat/completions",
        "gemini" => "https://generativelanguage.googleapis.com (auto-builds the rest)",
        _ => "http://localhost:11434"
    };

    partial void OnSelectedProviderChanged(AiProviderOption? value)
    {
        OnPropertyChanged(nameof(EndpointHint));
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

    /// <summary>
    /// Probes the configured provider with a small request to validate the connection.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            StatusMessage = "Testing…";
            var providerKey = SelectedProvider?.Key ?? "ollama";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            StatusMessage = await TestGenericProviderAsync(http, providerKey);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<string> TestGenericProviderAsync(HttpClient http, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(OllamaEndpoint))
            return "❌ Endpoint is empty.";

        try
        {
            // For ollama-like servers, probe /api/tags
            if (providerKey == "ollama")
            {
                var url = OllamaEndpoint.TrimEnd('/');
                if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                    url = url[..^4];
                var tagsRequest = new HttpRequestMessage(HttpMethod.Get, $"{url}/api/tags");
                if (!string.IsNullOrWhiteSpace(OllamaApiKey))
                    tagsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OllamaApiKey);
                using var tagsResp = await http.SendAsync(tagsRequest);
                var tagsJson = await tagsResp.Content.ReadAsStringAsync();
                if (!tagsResp.IsSuccessStatusCode)
                    return $"❌ Ollama returned {(int)tagsResp.StatusCode}: {tagsJson}";
                using var doc = JsonDocument.Parse(tagsJson);
                var count = doc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.GetArrayLength()
                    : 0;
                return $"✅ Ollama reachable · {count} model(s) installed.";
            }

            // For DeepSeek (OpenAI-compatible), list models
            if (providerKey == "deepseek")
            {
                var modelsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.deepseek.com/v1/models");
                if (!string.IsNullOrWhiteSpace(OllamaApiKey))
                    modelsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OllamaApiKey);
                using var resp = await http.SendAsync(modelsRequest);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return $"❌ DeepSeek returned {(int)resp.StatusCode}: {json}";
                using var doc = JsonDocument.Parse(json);
                var count = doc.RootElement.TryGetProperty("data", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.GetArrayLength()
                    : 0;
                return $"✅ DeepSeek reachable · {count} model(s) available.";
            }

            // For Gemini, list models with the API key
            if (providerKey == "gemini")
            {
                if (string.IsNullOrWhiteSpace(OllamaApiKey))
                    return "❌ Gemini requires an API key.";
                var modelsRequest = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
                modelsRequest.Headers.Add("x-goog-api-key", OllamaApiKey);
                using var resp = await http.SendAsync(modelsRequest);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return $"❌ Gemini returned {(int)resp.StatusCode}: {json}";
                using var doc = JsonDocument.Parse(json);
                var count = doc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.GetArrayLength()
                    : 0;
                return $"✅ Gemini reachable · {count} model(s) available.";
            }

            // For other cloud providers we just check DNS / host reachability via HEAD
            var uri = new Uri(OllamaEndpoint);
            var request = new HttpRequestMessage(HttpMethod.Head, $"{uri.Scheme}://{uri.Host}");
            using var response = await http.SendAsync(request);
            return $"✅ Host reachable ({(int)response.StatusCode} {response.ReasonPhrase}). " +
                   "Full validation will happen on first real call.";
        }
        catch (Exception ex)
        {
            return $"❌ {ex.Message}";
        }
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