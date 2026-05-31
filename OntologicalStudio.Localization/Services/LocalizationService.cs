using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OntologicalStudio.Localization.Services;

public class LocalizationService : ILocalizationService
{
    private string _currentLanguageCode = "en";
    private Dictionary<string, string> _translations = new Dictionary<string, string>();
    private Dictionary<string, string> _fallbackTranslations = new Dictionary<string, string>();
    private string _languagesDirectory = string.Empty;

    public event Action? OnLanguageChanged;

    public string CurrentLanguageCode => _currentLanguageCode;

    public LocalizationService(string languagesDirectory)
    {
        _languagesDirectory = languagesDirectory;
    }

    public void Initialize(string languagesDirectory)
    {
        _languagesDirectory = languagesDirectory;
        Directory.CreateDirectory(_languagesDirectory);
        LoadLanguagePack("en");
        _fallbackTranslations = new Dictionary<string, string>(_translations);
    }

    public async Task InitializeAsync(string languagesDirectory)
    {
        _languagesDirectory = languagesDirectory;
        Directory.CreateDirectory(_languagesDirectory);
        await LoadLanguagePackAsync("en");
        _fallbackTranslations = new Dictionary<string, string>(_translations);
    }

    public void LoadLanguagePack(string languageCode)
    {
        var languageFile = Path.Combine(_languagesDirectory, $"{languageCode}.json");

        if (File.Exists(languageFile))
        {
            var json = File.ReadAllText(languageFile);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            _translations = translations;
        }
        else
        {
            _translations = new Dictionary<string, string>();
        }

        _currentLanguageCode = languageCode;
        OnLanguageChanged?.Invoke();
    }

    public async Task LoadLanguagePackAsync(string languageCode)
    {
        var languageFile = Path.Combine(_languagesDirectory, $"{languageCode}.json");
        
        if (File.Exists(languageFile))
        {
            var json = await File.ReadAllTextAsync(languageFile);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            _translations = translations;
        }
        else
        {
            _translations = new Dictionary<string, string>();
        }

        _currentLanguageCode = languageCode;
        OnLanguageChanged?.Invoke();
    }

    public void ChangeLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        if (languageCode == _currentLanguageCode)
            return;

        try
        {
            var languageFile = Path.Combine(_languagesDirectory, $"{languageCode}.json");
            if (File.Exists(languageFile))
            {
                var json = File.ReadAllText(languageFile);
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                _translations = translations;
            }
            else
            {
                _translations = new Dictionary<string, string>();
            }

            _currentLanguageCode = languageCode;
            OnLanguageChanged?.Invoke();
        }
        catch
        {
        }
    }

    public string T(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (_translations.TryGetValue(key, out string? value))
        {
            return value ?? key;
        }

        if (_fallbackTranslations.TryGetValue(key, out string? fallbackValue))
        {
            return fallbackValue ?? key;
        }

        return key;
    }

    public string T(string key, params object[] args)
    {
        var translated = T(key);
        return string.Format(translated, args);
    }

    public IEnumerable<string> GetAvailableLanguages()
    {
        if (!Directory.Exists(_languagesDirectory))
            return new List<string> { "en" };

        var files = Directory.GetFiles(_languagesDirectory, "*.json");
        return files.Select(f => Path.GetFileNameWithoutExtension(f));
    }

    public async Task SaveLanguagePackAsync(string languageCode, Dictionary<string, string> translations)
    {
        var languageFile = Path.Combine(_languagesDirectory, $"{languageCode}.json");
        var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(languageFile, json);
    }

    public Dictionary<string, string> GetLoadedTranslations()
    {
        return _translations;
    }
}
