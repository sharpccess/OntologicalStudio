using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System.Text.Json;

namespace OntologicalStudio.Infrastructure.Services;

public class AiConnectionSettingsService : IAiConnectionSettingsService
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly string _settingsPath;
    private AiConnectionSettings _current = new();
    private bool _loaded;

    public event Action? SettingsChanged;

    public AiConnectionSettingsService()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OntologicalStudio");
        Directory.CreateDirectory(settingsDirectory);
        _settingsPath = Path.Combine(settingsDirectory, "ai-settings.json");
    }

    public async Task<AiConnectionSettings> GetAsync()
    {
        await EnsureLoadedAsync();
        return Clone(_current);
    }

    public AiConnectionSettings GetCurrent()
    {
        EnsureLoaded();
        return Clone(_current);
    }

    public async Task SaveAsync(AiConnectionSettings settings)
    {
        await EnsureLoadedAsync();
        var normalized = Normalize(settings);

        await _sync.WaitAsync();
        try
        {
            _current = Clone(normalized);
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        finally
        {
            _sync.Release();
        }

        SettingsChanged?.Invoke();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        _sync.Wait();
        try
        {
            if (_loaded)
                return;

            _current = LoadFromDisk();
            _loaded = true;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded)
            return;

        await _sync.WaitAsync();
        try
        {
            if (_loaded)
                return;

            _current = LoadFromDisk();
            _loaded = true;
        }
        finally
        {
            _sync.Release();
        }
    }

    private AiConnectionSettings LoadFromDisk()
    {
        if (!File.Exists(_settingsPath))
            return new AiConnectionSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AiConnectionSettings>(json);
            return Normalize(settings ?? new AiConnectionSettings());
        }
        catch
        {
            return new AiConnectionSettings();
        }
    }

    private static AiConnectionSettings Normalize(AiConnectionSettings settings)
    {
        var provider = settings.Provider?.Trim().ToLowerInvariant();
        var apiEndpoint = settings.ApiEndpoint?.Trim() ?? string.Empty;
        var apiKey = settings.ApiKey?.Trim() ?? string.Empty;
        var model = settings.Model?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiEndpoint))
            apiEndpoint = settings.OllamaEndpoint?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = settings.OllamaApiKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model))
            model = settings.OllamaModel?.Trim() ?? string.Empty;

        return new AiConnectionSettings
        {
            Provider = string.IsNullOrWhiteSpace(provider) ? "ollama" : provider,
            ApiEndpoint = apiEndpoint,
            ApiKey = apiKey,
            Model = model,
            OllamaEndpoint = apiEndpoint,
            OllamaApiKey = apiKey,
            OllamaModel = model
        };
    }

    private static AiConnectionSettings Clone(AiConnectionSettings settings)
    {
        return new AiConnectionSettings
        {
            Provider = settings.Provider,
            ApiEndpoint = settings.ApiEndpoint,
            ApiKey = settings.ApiKey,
            Model = settings.Model,
            OllamaEndpoint = settings.OllamaEndpoint,
            OllamaApiKey = settings.OllamaApiKey,
            OllamaModel = settings.OllamaModel
        };
    }
}