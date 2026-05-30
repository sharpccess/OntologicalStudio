namespace OntologicalStudio.Localization.Services;

public interface ILocalizationService
{
    string CurrentLanguageCode { get; }
    event Action OnLanguageChanged;
    Task InitializeAsync(string languagesDirectory);
    Task LoadLanguagePackAsync(string languageCode);
    void ChangeLanguage(string languageCode);
    string T(string key);
    string T(string key, params object[] args);
    IEnumerable<string> GetAvailableLanguages();
    Task SaveLanguagePackAsync(string languageCode, Dictionary<string, string> translations);
    Dictionary<string, string> GetLoadedTranslations();
}
