using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System.Text;

namespace OntologicalStudio.Application.Prompting;

public class PromptBuilder : IPromptBuilder
{
    private readonly IScenarioService _scenarios;
    private readonly IUniverseService _universes;
    private readonly IEntityService _entities;
    private readonly IRelationshipRepository _relationships;

    public PromptBuilder(
        IScenarioService scenarios,
        IUniverseService universes,
        IEntityService entities,
        IRelationshipRepository relationships)
    {
        _scenarios = scenarios;
        _universes = universes;
        _entities = entities;
        _relationships = relationships;
    }

    public async Task<string> BuildScenarioPromptAsync(Guid scenarioId, string? extraInstructions = null, string languageCode = "en")
    {
        var scenario = await _scenarios.GetByIdAsync(scenarioId)
            ?? throw new InvalidOperationException($"Scenario {scenarioId} not found.");
        var universe = await _universes.GetByIdAsync(scenario.UniverseId)
            ?? throw new InvalidOperationException($"Universe {scenario.UniverseId} not found.");
        var entities = (await _entities.GetByUniverseAsync(universe.Id)).ToList();

        var seen = new HashSet<Guid>();
        var rels = new List<Relationship>();
        foreach (var e in entities)
        {
            var rs = await _relationships.GetBySourceEntityAsync(e.Id);
            foreach (var r in rs)
                if (seen.Add(r.Id)) rels.Add(r);
        }

        return BuildScenarioPrompt(universe, scenario, entities, rels, extraInstructions, languageCode);
    }

    public string BuildScenarioPrompt(
        Universe universe,
        Scenario scenario,
        IReadOnlyList<Entity> entities,
        IReadOnlyList<Relationship> relationships,
        string? extraInstructions = null,
        string languageCode = "en")
    {
        var sb = new StringBuilder();
        var isSpanish = string.Equals(languageCode, "es", StringComparison.OrdinalIgnoreCase);

        sb.AppendLine(isSpanish ? "### MODELO DE CONTEXTO PARA RAZONAMIENTO" : "### CONTEXT MODEL FOR REASONING");
        sb.AppendLine(isSpanish ? $"**Universo:** {universe.Name}" : $"**Universe:** {universe.Name}");
        if (!string.IsNullOrWhiteSpace(universe.Description))
            sb.AppendLine(isSpanish ? $"**Descripción del universo:** {universe.Description}" : $"**Universe Description:** {universe.Description}");
        sb.AppendLine();
        sb.AppendLine(isSpanish ? $"**Título del problema:** {scenario.Title}" : $"**Problem Title:** {scenario.Title}");
        sb.AppendLine(isSpanish ? $"**Descripción de la situación:** {scenario.Description}" : $"**Situation Description:** {scenario.Description}");
        if (!string.IsNullOrWhiteSpace(scenario.Goals))
            sb.AppendLine(isSpanish ? $"**Objetivos y restricciones:** {scenario.Goals}" : $"**Objectives & Constraints:** {scenario.Goals}");
        sb.AppendLine();

        sb.AppendLine(isSpanish
            ? "### TAREA PRINCIPAL"
            : "### PRIMARY TASK");
        sb.AppendLine(isSpanish
            ? "Debes responder usando exclusivamente la información del universo, escenario, entidades y relaciones listadas abajo. Si la información es insuficiente, dilo explícitamente antes de proponer hipótesis."
            : "You must respond using only the universe, scenario, entities, and relationships listed below. If information is insufficient, say so explicitly before proposing hypotheses.");
        sb.AppendLine();

        sb.AppendLine(isSpanish ? "### ENTIDADES CLAVE" : "### KEY ENTITIES");
        if (entities.Count == 0)
            sb.AppendLine(isSpanish ? "_(todavía no hay entidades definidas)_" : "_(no entities defined yet)_");
        foreach (var e in entities)
        {
            sb.AppendLine($"- **{e.Name}** ({e.EntityType?.DisplayName ?? e.EntityType?.Name ?? (isSpanish ? "Entidad" : "Entity")})");
            if (!string.IsNullOrWhiteSpace(e.Description))
                sb.AppendLine(isSpanish ? $"  Descripción: {e.Description}" : $"  Description: {e.Description}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine(isSpanish ? $"  Notas: {e.Notes}" : $"  Notes: {e.Notes}");
            sb.AppendLine(isSpanish
                ? $"  Confianza: {e.ConfidenceLevel}% | Completitud: {e.CompletenessScore}%"
                : $"  Confidence: {e.ConfidenceLevel}% | Completeness: {e.CompletenessScore}%");
            if (!string.IsNullOrWhiteSpace(e.HydrationData))
                sb.AppendLine(isSpanish ? $"  Datos de hidratación: {e.HydrationData}" : $"  Hydration data: {e.HydrationData}");
        }
        sb.AppendLine();

        sb.AppendLine(isSpanish ? "### RELACIONES" : "### RELATIONSHIPS");
        if (relationships.Count == 0)
            sb.AppendLine(isSpanish ? "_(todavía no hay relaciones definidas)_" : "_(no relationships defined yet)_");
        foreach (var r in relationships)
        {
            var src = entities.FirstOrDefault(x => x.Id == r.SourceEntityId)?.Name ?? "?";
            var tgt = entities.FirstOrDefault(x => x.Id == r.TargetEntityId)?.Name ?? "?";
            var type = r.RelationshipType?.DisplayName ?? r.RelationshipType?.Name ?? "relatesTo";
            sb.AppendLine($"- {src} --({type})--> {tgt}");
            if (!string.IsNullOrWhiteSpace(r.Description))
                sb.AppendLine($"  {r.Description}");
        }
        sb.AppendLine();

        sb.AppendLine(isSpanish ? "### INSTRUCCIONES DE RAZONAMIENTO" : "### REASONING INSTRUCTIONS");
        if (isSpanish)
        {
            sb.AppendLine("Responde exclusivamente en español. Actúa como un analista estratégico de sistemas y consultor práctico.");
            sb.AppendLine("Devuelve la respuesta en este formato exacto:");
            sb.AppendLine("1. Resumen ejecutivo");
            sb.AppendLine("2. Dinámica principal del sistema");
            sb.AppendLine("3. Riesgos y contradicciones");
            sb.AppendLine("4. Recomendaciones concretas priorizadas");
            sb.AppendLine("5. Próximos pasos accionables");
            sb.AppendLine("No inventes entidades ni relaciones que no estén en el modelo.");
        }
        else
        {
            sb.AppendLine("Respond exclusively in English. Act as a strategic systems analyst and practical consultant.");
            sb.AppendLine("Return the answer in this exact structure:");
            sb.AppendLine("1. Executive summary");
            sb.AppendLine("2. Main system dynamic");
            sb.AppendLine("3. Risks and contradictions");
            sb.AppendLine("4. Prioritized concrete recommendations");
            sb.AppendLine("5. Actionable next steps");
            sb.AppendLine("Do not invent entities or relationships that are not present in the model.");
        }

        if (!string.IsNullOrWhiteSpace(extraInstructions))
        {
            sb.AppendLine();
            sb.AppendLine(isSpanish ? "### INSTRUCCIONES ADICIONALES" : "### ADDITIONAL INSTRUCTIONS");
            sb.AppendLine(extraInstructions);
        }

        sb.AppendLine();
        sb.AppendLine(isSpanish
            ? "### REGLA FINAL DE IDIOMA"
            : "### FINAL LANGUAGE RULE");
        sb.AppendLine(isSpanish
            ? "Toda la respuesta debe estar en español."
            : "The entire response must be in English.");
        return sb.ToString();
    }
}
