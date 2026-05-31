using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OntologicalStudio.AIProviders;

public class ConfigurableAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    
    public string ProviderName => "Configurable AI Provider (OpenAI/Ollama/Heuristic)";

    public ConfigurableAIProvider()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(openAiKey)) return true;

        try
        {
            // Check if Ollama is running locally
            var response = await _httpClient.GetAsync("http://localhost:11434/");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GeneratePromptAsync(PromptContext context)
    {
        var builder = new StringBuilder();
        await foreach (var chunk in StreamAsync(new AIRequest
        {
            UserPrompt = context.CurrentContext,
            SystemPrompt = context.OutputFormat,
            OutputFormat = context.OutputFormat,
            JsonMode = false
        }))
        {
            if (chunk is TextChunk textChunk)
                builder.Append(textChunk.Text);
        }
        return builder.ToString();
    }

    public async Task<HydrationResult> HydrateEntityAsync(Entity entity, HydrationOptions options, string? customPrompt = null, string languageCode = "en", WebResearchResult? webResearch = null)
    {
        var isSpanish = string.Equals(languageCode, "es", StringComparison.OrdinalIgnoreCase);
        var researchBlock = BuildResearchBlock(webResearch, isSpanish);
        string prompt = string.IsNullOrWhiteSpace(customPrompt)
            ? isSpanish
                ? $"Completa atributos, notas y dinámicas para la entidad '{entity.Name}' de tipo '{entity.EntityType?.Name ?? "General"}'. Descripción actual: {entity.Description}. Notas actuales: {entity.Notes}.{Environment.NewLine}{researchBlock}"
                : $"Complete attributes, notes, and dynamics for the entity '{entity.Name}' of type '{entity.EntityType?.Name ?? "General"}'. Current description: {entity.Description}. Current notes: {entity.Notes}.{Environment.NewLine}{researchBlock}"
            : isSpanish
                ? $"Hidrata la entidad '{entity.Name}' de tipo '{entity.EntityType?.Name ?? "General"}' siguiendo esta instrucción: {customPrompt}{Environment.NewLine}{Environment.NewLine}Descripción actual: {entity.Description}{Environment.NewLine}Notas actuales: {entity.Notes}{Environment.NewLine}{researchBlock}"
                : $"Hydrate the entity '{entity.Name}' of type '{entity.EntityType?.Name ?? "General"}' following this instruction: {customPrompt}{Environment.NewLine}{Environment.NewLine}Current description: {entity.Description}{Environment.NewLine}Current notes: {entity.Notes}{Environment.NewLine}{researchBlock}";

        string system = isSpanish
            ? "Eres un consultor experto en ontologías, mapeo sistémico e investigación web. Usa el contexto de investigación web si está disponible. Responde SIEMPRE en español. Devuelve SOLO un JSON con: confidenceScore (int 0-100), completenessScore (int 0-100), suggestedProperties (objeto JSON de atributos), y analysisNotes (string). analysisNotes debe ser un enriquecimiento breve pero útil para la descripción de la entidad."
            : "You are an expert ontology, systems-mapping, and web-research consultant. Use the web research context when available. Respond ONLY in English. Return ONLY a JSON object with: confidenceScore (int 0-100), completenessScore (int 0-100), suggestedProperties (JSON object of attributes), and analysisNotes (string). analysisNotes must be a concise but useful enrichment for the entity description.";
        
        string rawResponse = string.Empty;
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? ollamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";

        if (!string.IsNullOrEmpty(openAiKey))
        {
            try
            {
                rawResponse = await GenerateOpenAiAsync(prompt, system, openAiKey, true);
            }
            catch { }
        }
        
        if (string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                rawResponse = await GenerateOllamaAsync(prompt, system, ollamaEndpoint, true);
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                var jsonPayload = ExtractJsonObject(rawResponse);
                var node = JsonNode.Parse(jsonPayload);
                if (node != null)
                {
                    var analysisNotes = node["analysisNotes"]?.GetValue<string>()?.Trim();
                    var suggestedPropertiesJson = node["suggestedProperties"]?.ToString() ?? "{}";
                    return new HydrationResult
                    {
                        ConfidenceScore = node["confidenceScore"]?.GetValue<int>() ?? 70,
                        CompletenessScore = node["completenessScore"]?.GetValue<int>() ?? 75,
                        SuggestedProperties = suggestedPropertiesJson,
                        SuggestedPropertiesJson = suggestedPropertiesJson,
                        SuggestedNotes = analysisNotes ?? (isSpanish ? "Completado mediante IA." : "Completed via AI."),
                        AnalysisNotes = analysisNotes ?? (isSpanish ? "Completado mediante IA." : "Completed via AI.")
                    };
                }
            }
            catch { }
        }

        // Offline Heuristic Fallback
        return GenerateHeuristicHydration(entity);
    }

    public async IAsyncEnumerable<AIChunk> StreamAsync(
        AIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string text = string.Empty;
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? ollamaEndpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";

        if (!string.IsNullOrEmpty(openAiKey))
        {
            try
            {
                text = await GenerateOpenAiAsync(request.UserPrompt, request.SystemPrompt, openAiKey, request.JsonMode);
            }
            catch (Exception ex)
            {
                text = $"OpenAI error (falling back): {ex.Message}\n\n" + GenerateHeuristicAnalysis(request.UserPrompt);
            }
        }
        else
        {
            try
            {
                text = await GenerateOllamaAsync(request.UserPrompt, request.SystemPrompt, ollamaEndpoint, request.JsonMode);
            }
            catch
            {
                text = GenerateHeuristicAnalysis(request.UserPrompt);
            }
        }

        foreach (var chunk in ChunkText(text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new TextChunk(chunk);
            await Task.Delay(20, cancellationToken);
        }

        yield return new DoneChunk(request.UserPrompt.Length / 4, text.Length / 4);
    }

    public async Task<IEnumerable<RelationshipSuggestion>> SuggestRelationshipsAsync(Entity entity)
    {
        await Task.Delay(200); // Simulate network latency
        return new List<RelationshipSuggestion>
        {
            new() { RelationshipTypeName = "influences", TargetEntityId = Guid.NewGuid(), Confidence = 85, Description = "AI-suggested influence vector." }
        };
    }

    // OpenAI Client Implementation
    private async Task<string> GenerateOpenAiAsync(string prompt, string systemPrompt, string apiKey, bool jsonMode = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            },
            response_format = jsonMode ? new { type = "json_object" } : null,
            temperature = 0.7
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    // Ollama Client Implementation
    private async Task<string> GenerateOllamaAsync(string prompt, string systemPrompt, string endpoint, bool jsonMode = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/api/generate");

        var body = new
        {
            model = "llama3",
            system = systemPrompt,
            prompt = prompt,
            format = jsonMode ? "json" : null,
            stream = false,
            options = new { temperature = 0.7 }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }

    // Heuristic Seeding Engine
    private HydrationResult GenerateHeuristicHydration(Entity entity)
    {
        var properties = new Dictionary<string, string>();
        string notes = string.Empty;
        int conf = 75;
        int comp = 80;

        string type = entity.EntityType?.Name ?? "General";

        switch (type.ToUpper())
        {
            case "CEO":
                properties.Add("Authority Style", "Top-down directive");
                properties.Add("Operational Dependency", "High risk of bottleneck");
                properties.Add("Key Incentives", "Equity performance and autonomy");
                notes = "This leadership entity shows potential resistance to delegating operational decision-making, which creates downstream bottlenecks with managers and field teams.";
                break;
            case "FOUNDER":
                properties.Add("Attachment level", "Extreme sentimental ownership");
                properties.Add("Vision Alignment", "Highly centered on legacy product lines");
                properties.Add("Key Blocker", "Resists changes to organizational structure");
                notes = "Founder presents high emotional attachment to early systems. May exhibit blind spots regarding modern governance scaling and strategic restructuring.";
                break;
            case "BELIEF":
                properties.Add("Origin", "Historical parental framework");
                properties.Add("Rigidity", "9/10");
                properties.Add("Impact on Decisions", "Causes self-sabotage in expansion efforts");
                notes = "Core internalized conviction. Acting as a silent filter that frames challenges as insurmountable risks rather than manageable obstacles.";
                break;
            case "FEAR":
                properties.Add("Underlying Threat", "Loss of authority / fear of failure");
                properties.Add("Coping Mechanism", "Micromanagement and over-analysis");
                notes = "Fear of losing control leads to information hoarding, stalling strategic initiatives and reducing trust across teams.";
                break;
            default:
                properties.Add("Core Function", "System node representing resource or concept");
                properties.Add("Tension Impact", "Moderate");
                notes = "A general system actor. Standard behavioral patterns apply under systemic stress conditions.";
                break;
        }

        return new HydrationResult
        {
            ConfidenceScore = conf,
            CompletenessScore = comp,
            SuggestedProperties = JsonSerializer.Serialize(properties),
            SuggestedPropertiesJson = JsonSerializer.Serialize(properties),
            SuggestedNotes = notes,
            AnalysisNotes = notes
        };
    }

    private static string BuildResearchBlock(WebResearchResult? webResearch, bool isSpanish)
    {
        if (webResearch is null || string.IsNullOrWhiteSpace(webResearch.Summary))
            return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine(isSpanish ? "Contexto de investigación web reciente:" : "Recent web research context:");
        builder.AppendLine(webResearch.Summary);
        if (webResearch.Sources.Count > 0)
        {
            builder.AppendLine(isSpanish ? "Fuentes:" : "Sources:");
            foreach (var source in webResearch.Sources.Take(5))
                builder.AppendLine($"- {source.Title}: {source.Url}");
        }

        return builder.ToString().Trim();
    }

    private static string ExtractJsonObject(string rawResponse)
    {
        var trimmed = rawResponse.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return trimmed[firstBrace..(lastBrace + 1)];

        return trimmed;
    }

    private string GenerateHeuristicAnalysis(string promptTemplate)
    {
        var isSpanish = promptTemplate.Contains("Toda la respuesta debe estar en español.", StringComparison.OrdinalIgnoreCase)
            || promptTemplate.Contains("Responde exclusivamente en español.", StringComparison.OrdinalIgnoreCase);

        // Extract scenario name if possible
        string scenarioName = "Active System modeling";
        if (promptTemplate.Contains("**Problem Title:**"))
        {
            var lines = promptTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var titleLine = lines.FirstOrDefault(l => l.Contains("**Problem Title:**"));
            if (titleLine != null)
            {
                scenarioName = titleLine.Replace("**Problem Title:**", "").Trim();
            }
        }
        else if (promptTemplate.Contains("**Título del problema:**"))
        {
            var lines = promptTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var titleLine = lines.FirstOrDefault(l => l.Contains("**Título del problema:**"));
            if (titleLine != null)
            {
                scenarioName = titleLine.Replace("**Título del problema:**", "").Trim();
            }
        }

        var sb = new StringBuilder();
        if (isSpanish)
        {
            sb.AppendLine($"# DIAGNÓSTICO SISTÉMICO: {scenarioName.ToUpper()}");
            sb.AppendLine();
            sb.AppendLine("## 1. Resumen ejecutivo");
            sb.AppendLine("La situación descrita muestra un sistema con dependencias cruzadas, tensiones entre objetivos y señales de bloqueo en la toma de decisiones. El modelo sugiere que el problema no es aislado, sino producto de dinámicas acumuladas entre actores, creencias y restricciones.");
            sb.AppendLine();
            sb.AppendLine("## 2. Dinámica principal del sistema");
            sb.AppendLine("- Hay interdependencias que convierten decisiones pequeñas en efectos amplificados.");
            sb.AppendLine("- La relación entre actores clave parece generar fricción, retraso o contradicción operativa.");
            sb.AppendLine();
            sb.AppendLine("## 3. Riesgos y contradicciones");
            sb.AppendLine("- Existen incentivos, miedos o supuestos que pueden estar empujando al sistema en dirección opuesta al objetivo declarado.");
            sb.AppendLine("- Si no se corrige, el sistema tenderá a repetir el mismo patrón de bloqueo.");
            sb.AppendLine();
            sb.AppendLine("## 4. Recomendaciones concretas priorizadas");
            sb.AppendLine("1. Aclarar responsabilidades y límites entre las entidades más influyentes.");
            sb.AppendLine("2. Identificar qué relación o supuesto está produciendo mayor fricción.");
            sb.AppendLine("3. Probar una intervención pequeña y medible antes de rediseñar todo el sistema.");
            sb.AppendLine();
            sb.AppendLine("## 5. Próximos pasos accionables");
            sb.AppendLine("1. Confirmar qué entidades tienen mayor impacto en el problema actual.");
            sb.AppendLine("2. Revisar si las relaciones dibujadas reflejan el estado real o necesitan ajuste.");
            sb.AppendLine("3. Ejecutar una prueba corta con una intervención prioritaria y evaluar resultados.");
        }
        else
        {
            sb.AppendLine($"# SYSTEMIC DIAGNOSIS: {scenarioName.ToUpper()}");
            sb.AppendLine();
            sb.AppendLine("## 1. Executive summary");
            sb.AppendLine("The described situation reflects a system with cross-dependencies, tensions between goals, and signs of decision bottlenecks. The model suggests the problem is systemic rather than isolated.");
            sb.AppendLine();
            sb.AppendLine("## 2. Main system dynamic");
            sb.AppendLine("- Interdependencies appear to turn small decisions into amplified effects.");
            sb.AppendLine("- Relationships between key actors seem to create friction, delays, or operational contradiction.");
            sb.AppendLine();
            sb.AppendLine("## 3. Risks and contradictions");
            sb.AppendLine("- Incentives, fears, or assumptions may be pushing the system away from its stated objective.");
            sb.AppendLine("- If nothing changes, the same blocking pattern is likely to repeat.");
            sb.AppendLine();
            sb.AppendLine("## 4. Prioritized concrete recommendations");
            sb.AppendLine("1. Clarify responsibilities and boundaries among the most influential entities.");
            sb.AppendLine("2. Identify which relationship or assumption is generating the highest friction.");
            sb.AppendLine("3. Test a small measurable intervention before redesigning the whole system.");
            sb.AppendLine();
            sb.AppendLine("## 5. Actionable next steps");
            sb.AppendLine("1. Confirm which entities have the strongest impact on the current problem.");
            sb.AppendLine("2. Review whether the mapped relationships reflect the real current state.");
            sb.AppendLine("3. Run a short pilot with one prioritized intervention and evaluate results.");
        }
        
        return sb.ToString();
    }

    private static IEnumerable<string> ChunkText(string text, int chunkSize = 160)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        for (var index = 0; index < text.Length; index += chunkSize)
            yield return text.Substring(index, Math.Min(chunkSize, text.Length - index));
    }
}
