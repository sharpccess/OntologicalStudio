using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Core.Interfaces;

public interface IAiConnectionSettingsService
{
    event Action? SettingsChanged;
    Task<AiConnectionSettings> GetAsync();
    AiConnectionSettings GetCurrent();
    Task SaveAsync(AiConnectionSettings settings);
}