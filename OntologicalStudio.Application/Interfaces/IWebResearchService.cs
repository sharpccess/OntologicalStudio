using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IWebResearchService
{
    Task<WebResearchResult?> ResearchAsync(string query, string languageCode, CancellationToken cancellationToken = default);
}