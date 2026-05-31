using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Localization.Services;
using System.Net.Http;
using System.Text;
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

    /// <summary>
    /// Probes the configured provider with a tiny request to validate the connection.
    /// For the VSCode/TRAE bridge it hits /health and /models; for other providers it
    /// fires a short "ping" prompt.
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

            if (providerKey == "vscode")
            {
                StatusMessage = await TestVsCodeBridgeAsync(OllamaEndpoint);
            }
            else
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                StatusMessage = await TestGenericProviderAsync(http, providerKey);
            }
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

    private static async Task<string> TestVsCodeBridgeAsync(string endpoint)
    {
        var baseUrl = NormalizeBridgeBaseUrl(endpoint);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "❌ Empty endpoint. Use e.g. http://localhost:39217";

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        try
        {
            var healthJson = await http.GetStringAsync($"{baseUrl}/health");
            using var hDoc = JsonDocument.Parse(healthJson);
            var status = hDoc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : "?";
            var port = hDoc.RootElement.TryGetProperty("port", out var p) ? p.GetInt32().ToString() : "?";

            var modelsJson = await http.GetStringAsync($"{baseUrl}/models");
            using var mDoc = JsonDocument.Parse(modelsJson);
            var count = mDoc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array
                ? arr.GetArrayLength()
                : 0;

            if (count == 0)
                return $"⚠️  Bridge reachable on port {port} ({status}) but exposes 0 models. " +
                       "Make sure Copilot or another chat provider is signed in.";

            // Pick a friendly preview of the first model
            var first = arr.EnumerateArray().First();
            var vendor = first.TryGetProperty("vendor", out var v) ? v.GetString() : "?";
            var family = first.TryGetProperty("family", out var f) ? f.GetString() : "?";
            return $"✅ Bridge OK · port {port} · {count} model(s) · default: {vendor}/{family}";
        }
        catch (HttpRequestException ex)
        {
            return $"❌ Cannot reach bridge at {baseUrl}. Is VSCode/TRAE running with the extension enabled? ({ex.Message})";
        }
        catch (TaskCanceledException)
        {
            return $"❌ Timeout contacting {baseUrl}. Check that the bridge is started.";
        }
    }

    private async Task<string> TestGenericProviderAsync(HttpClient http, string providerKey)
    {
        // Simple HEAD/GET on the host to confirm reachability when possible.
        if (string.IsNullOrWhiteSpace(OllamaEndpoint))
            return "❌ Endpoint is empty.";

        try
        {
            // For ollama-like local servers, probe /api/tags
            if (providerKey == "ollama")
            {
                var url = OllamaEndpoint.TrimEnd('/');
                if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                    url = url[..^4];
                var tagsJson = await http.GetStringAsync($"{url}/api/tags");
                using var doc = JsonDocument.Parse(tagsJson);
                var count = doc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.GetArrayLength()
                    : 0;
                return $"✅ Ollama reachable · {count} model(s) installed.";
            }

            // For cloud providers we just check DNS reachability via HEAD on the host
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

    private static string NormalizeBridgeBaseUrl(string endpoint)
    {
        var normalized = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            normalized = $"http://{normalized}";
        normalized = normalized.TrimEnd('/');
        // Strip /chat if user pasted the full chat endpoint
        if (normalized.EndsWith("/chat", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^5];
        return normalized;
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