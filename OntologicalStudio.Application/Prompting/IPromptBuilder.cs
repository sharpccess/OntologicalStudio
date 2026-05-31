using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Prompting;

public interface IPromptBuilder
{
    /// <summary>
    /// Builds a fully contextualized reasoning prompt from a universe + scenario + ontology.
    /// Pure / deterministic; does not call any AI provider.
    /// </summary>
    Task<string> BuildScenarioPromptAsync(Guid scenarioId, string? extraInstructions = null, string languageCode = "en");

    string BuildScenarioPrompt(
        Universe universe,
        Scenario scenario,
        IReadOnlyList<Entity> entities,
        IReadOnlyList<Relationship> relationships,
        string? extraInstructions = null,
        string languageCode = "en");
}
