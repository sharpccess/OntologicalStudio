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
    private readonly IAiConnectionSettingsService _settingsService;
    private string _providerName = "Configurable AI Provider";
    
    public string ProviderName => _providerName;

    public ConfigurableAIProvider(IAiConnectionSettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        var configuredProvider = await GetConfiguredProviderAsync();
        if (configuredProvider.IsConfigured)
        {
            _providerName = BuildProviderDisplayName(configuredProvider.Provider, configuredProvider.Model ?? "configured");
            return true;
        }

        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(openAiKey))
        {
            _providerName = "OpenAI (gpt-4o-mini)";
            return true;
        }

        try
        {
            var fallbackOllama = GetFallbackOllamaConfiguration();
            var model = await ResolveOllamaModelAsync(fallbackOllama.Endpoint, fallbackOllama.ApiKey, fallbackOllama.Model);
            _providerName = BuildOllamaProviderName(fallbackOllama.Endpoint, model);
            return true;
        }
        catch
        {
            _providerName = "Heuristic fallback";
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
        var prompt = BuildHydrationPrompt(entity, options, customPrompt, webResearch, isSpanish);
        var system = BuildHydrationSystemPrompt(isSpanish);
        var promptUsed = BuildPromptTranscript(system, prompt);
        var configuredProvider = await GetConfiguredProviderAsync();
        
        string rawResponse = string.Empty;
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Exception? lastError = null;

        if (configuredProvider.IsConfigured)
        {
            try
            {
                rawResponse = await GenerateConfiguredProviderAsync(
                    prompt,
                    system,
                    configuredProvider,
                    false);
            }
            catch (Exception ex)
            {
                throw BuildConfiguredProviderException(configuredProvider, ex);
            }

            if (string.IsNullOrWhiteSpace(rawResponse))
                throw BuildConfiguredProviderException(configuredProvider, new InvalidOperationException("The provider returned an empty response."));
        }

        if (string.IsNullOrEmpty(rawResponse) && !string.IsNullOrEmpty(openAiKey))
        {
            try
            {
                rawResponse = await GenerateOpenAiAsync(prompt, system, openAiKey, false);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }
        
        if (string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                var fallbackOllama = GetFallbackOllamaConfiguration();
                rawResponse = await GenerateOllamaAsync(
                    prompt,
                    system,
                    fallbackOllama.Endpoint,
                    fallbackOllama.Model,
                    fallbackOllama.ApiKey,
                    false);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (!string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                var parsedResult = TryParseStructuredHydrationResponse(
                    rawResponse,
                    prompt,
                    customPrompt,
                    promptUsed,
                    webResearch,
                    isSpanish);

                if (parsedResult is not null)
                    return parsedResult;

                var plainTextResult = BuildPlainTextHydrationResult(
                    rawResponse,
                    prompt,
                    customPrompt,
                    promptUsed,
                    webResearch,
                    isSpanish);

                if (plainTextResult is not null)
                    return plainTextResult;
            }
            catch { }
        }

        if (configuredProvider.IsConfigured)
            throw BuildConfiguredProviderException(
                configuredProvider,
                new InvalidOperationException("The provider returned content, but it could not be interpreted as a valid hydration response."));

        _providerName = lastError is null
            ? "Heuristic fallback"
            : $"Heuristic fallback ({lastError.Message})";

        return GenerateHeuristicHydration(entity, options, isSpanish, promptUsed, webResearch, customPrompt);
    }

    public async IAsyncEnumerable<AIChunk> StreamAsync(
        AIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string text = string.Empty;
        var configuredProvider = await GetConfiguredProviderAsync();
        string? openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Exception? lastError = null;

        if (configuredProvider.IsConfigured)
        {
            try
            {
                text = await GenerateConfiguredProviderAsync(
                    request.UserPrompt,
                    request.SystemPrompt,
                    configuredProvider,
                    request.JsonMode);
            }
            catch (Exception ex)
            {
                throw BuildConfiguredProviderException(configuredProvider, ex);
            }

            if (string.IsNullOrWhiteSpace(text))
                throw BuildConfiguredProviderException(configuredProvider, new InvalidOperationException("The provider returned an empty response."));
        }

        if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(openAiKey))
        {
            try
            {
                text = await GenerateOpenAiAsync(request.UserPrompt, request.SystemPrompt, openAiKey, request.JsonMode);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            try
            {
                var fallbackOllama = GetFallbackOllamaConfiguration();
                text = await GenerateOllamaAsync(
                    request.UserPrompt,
                    request.SystemPrompt,
                    fallbackOllama.Endpoint,
                    fallbackOllama.Model,
                    fallbackOllama.ApiKey,
                    request.JsonMode);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            _providerName = lastError is null
                ? "Heuristic fallback"
                : $"Heuristic fallback ({lastError.Message})";
            text = GenerateHeuristicAnalysis(request.UserPrompt);
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
        _providerName = "OpenAI (gpt-4o-mini)";
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

    private async Task<string> GenerateConfiguredProviderAsync(string prompt, string systemPrompt, ProviderConfiguration provider, bool jsonMode)
    {
        var normalizedProvider = (provider.Provider ?? string.Empty).Trim().ToLowerInvariant();

        return normalizedProvider switch
        {
            "openrouter" => await GenerateOpenRouterAsync(prompt, systemPrompt, provider, jsonMode),
            "openai" => await GenerateOpenAiCompatibleAsync(prompt, systemPrompt, provider, jsonMode, "OpenAI"),
            "anthropic" => await GenerateAnthropicAsync(prompt, systemPrompt, provider),
            "vscode" => await GenerateVsCodeBridgeAsync(prompt, systemPrompt, provider),
            _ => await GenerateOllamaAsync(prompt, systemPrompt, provider.Endpoint, provider.Model, provider.ApiKey, jsonMode)
        };
    }

    // VSCode / TRAE Bridge: posts the prompt to the local extension server
    // which forwards it through vscode.lm (Copilot, Claude, etc.).
    private async Task<string> GenerateVsCodeBridgeAsync(string prompt, string systemPrompt, ProviderConfiguration provider)
    {
        var endpoint = NormalizeVsCodeBridgeEndpoint(provider.Endpoint);
        var modelLabel = string.IsNullOrWhiteSpace(provider.Model) ? "auto" : provider.Model!;
        _providerName = $"VSCode/TRAE Bridge ({modelLabel})";

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        var body = new
        {
            systemPrompt = systemPrompt ?? string.Empty,
            userPrompt = prompt ?? string.Empty,
            model = string.IsNullOrWhiteSpace(provider.Model) ? null : provider.Model
        };
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var jsonString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"VSCode bridge returned {(int)response.StatusCode}: {jsonString}");

        using var doc = JsonDocument.Parse(jsonString);
        if (doc.RootElement.TryGetProperty("model", out var modelProp))
        {
            var resolvedModel = modelProp.GetString();
            if (!string.IsNullOrWhiteSpace(resolvedModel))
                _providerName = $"VSCode/TRAE Bridge ({resolvedModel})";
        }
        return doc.RootElement.TryGetProperty("content", out var contentProp)
            ? contentProp.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string NormalizeVsCodeBridgeEndpoint(string endpoint)
    {
        var normalized = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "http://localhost:39217";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        normalized = normalized.TrimEnd('/');
        if (!normalized.EndsWith("/chat", StringComparison.OrdinalIgnoreCase))
            normalized = $"{normalized}/chat";

        return normalized;
    }

    private async Task<string> GenerateOpenRouterAsync(string prompt, string systemPrompt, ProviderConfiguration provider, bool jsonMode)
    {
        var endpoint = NormalizeChatCompletionsEndpoint(provider.Endpoint, "https://openrouter.ai/api/v1/chat/completions");
        _providerName = $"OpenRouter ({provider.Model})";
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        request.Headers.Add("HTTP-Referer", "https://ontologicalstudio.local");
        request.Headers.Add("X-Title", "Ontological Studio");

        var body = new
        {
            model = provider.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            response_format = jsonMode ? new { type = "json_object" } : null
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private async Task<string> GenerateOpenAiCompatibleAsync(string prompt, string systemPrompt, ProviderConfiguration provider, bool jsonMode, string providerLabel)
    {
        var endpoint = NormalizeChatCompletionsEndpoint(provider.Endpoint, "https://api.openai.com/v1/chat/completions");
        _providerName = $"{providerLabel} ({provider.Model})";
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

        var body = new
        {
            model = provider.Model,
            messages = new object[]
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
        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private async Task<string> GenerateAnthropicAsync(string prompt, string systemPrompt, ProviderConfiguration provider)
    {
        var endpoint = NormalizeAnthropicEndpoint(provider.Endpoint);
        _providerName = $"Anthropic ({provider.Model})";
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("x-api-key", provider.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = provider.Model,
            system = systemPrompt,
            max_tokens = 2048,
            temperature = 0.7,
            messages = new object[]
            {
                new { role = "user", content = prompt }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in contentArray.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeProp)
                    && typeProp.GetString() == "text"
                    && item.TryGetProperty("text", out var textProp))
                {
                    return textProp.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    // Ollama Client Implementation
    private async Task<string> GenerateOllamaAsync(string prompt, string systemPrompt, string endpoint, string? configuredModel, string? apiKey, bool jsonMode = false)
    {
        var normalizedEndpoint = NormalizeOllamaEndpoint(endpoint);
        var model = await ResolveOllamaModelAsync(normalizedEndpoint, apiKey, configuredModel);
        _providerName = BuildOllamaProviderName(normalizedEndpoint, model);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{normalizedEndpoint}/api/generate");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
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

    private async Task<string> ResolveOllamaModelAsync(string endpoint, string? apiKey, string? configuredModel)
    {
        if (!string.IsNullOrWhiteSpace(configuredModel))
            return configuredModel.Trim();

        var configured = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var normalizedEndpoint = NormalizeOllamaEndpoint(endpoint);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{normalizedEndpoint}/api/tags");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Ollama returned no installed models.");

        var preferredFamilies = new[] { "qwen", "phi3", "llama3", "mistral" };
        var available = new List<string>();

        foreach (var model in models.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var nameProperty))
            {
                var name = nameProperty.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    available.Add(name);
            }
        }

        foreach (var family in preferredFamilies)
        {
            var match = available.FirstOrDefault(x => x.StartsWith(family, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
                return match;
        }

        var firstAvailable = available.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstAvailable))
            return firstAvailable;

        throw new InvalidOperationException("No Ollama models are installed.");
    }

    private async Task<ProviderConfiguration> GetConfiguredProviderAsync()
    {
        var settings = await _settingsService.GetAsync();
        var provider = (settings.Provider ?? string.Empty).Trim().ToLowerInvariant();
        // VSCode bridge does not require an API key (auth is delegated to the editor's LM API).
        var isConfigured = provider switch
        {
            "vscode" => !string.IsNullOrWhiteSpace(settings.ApiEndpoint),
            _ => !string.IsNullOrWhiteSpace(settings.Provider)
                && !string.IsNullOrWhiteSpace(settings.ApiEndpoint)
                && !string.IsNullOrWhiteSpace(settings.Model)
                && !string.IsNullOrWhiteSpace(settings.ApiKey)
        };
        return new ProviderConfiguration(
            settings.Provider,
            settings.ApiEndpoint,
            settings.Model,
            settings.ApiKey,
            isConfigured);
    }

    private static OllamaConfiguration GetFallbackOllamaConfiguration()
    {
        return new OllamaConfiguration(
            Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434",
            Environment.GetEnvironmentVariable("OLLAMA_MODEL"),
            Environment.GetEnvironmentVariable("OLLAMA_API_KEY"),
            false);
    }

    private static string NormalizeOllamaEndpoint(string endpoint)
    {
        var normalized = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "http://localhost:11434";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                ? $"http://{normalized}"
                : $"https://{normalized}";
        }

        normalized = normalized.TrimEnd('/');
        if (normalized.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4];

        return normalized.TrimEnd('/');
    }

    private static string BuildOllamaProviderName(string endpoint, string model)
    {
        return endpoint.Contains("ollama.com", StringComparison.OrdinalIgnoreCase)
            ? $"Ollama Cloud ({model})"
            : $"Ollama ({model})";
    }

    private static string BuildProviderDisplayName(string provider, string model)
    {
        return provider switch
        {
            "openrouter" => $"OpenRouter ({model})",
            "openai" => $"OpenAI ({model})",
            "anthropic" => $"Anthropic ({model})",
            "vscode" => $"VSCode/TRAE Bridge ({model})",
            _ => $"Ollama ({model})"
        };
    }

    private InvalidOperationException BuildConfiguredProviderException(ProviderConfiguration provider, Exception ex)
    {
        var name = BuildProviderDisplayName(provider.Provider, provider.Model ?? "configured");
        return new InvalidOperationException($"{name} failed: {ex.Message}", ex);
    }

    private static string NormalizeChatCompletionsEndpoint(string endpoint, string fallback)
    {
        var normalized = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = fallback;

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"https://{normalized}";
        }

        normalized = normalized.TrimEnd('/');
        if (!normalized.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                normalized = $"{normalized}/chat/completions";
            else if (!normalized.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
                normalized = $"{normalized}/v1/chat/completions";
        }

        return normalized;
    }

    private static string NormalizeAnthropicEndpoint(string endpoint)
    {
        var normalized = (endpoint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "https://api.anthropic.com/v1/messages";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"https://{normalized}";
        }

        normalized = normalized.TrimEnd('/');
        if (!normalized.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
        {
            if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                normalized = $"{normalized}/messages";
            else if (!normalized.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
                normalized = $"{normalized}/v1/messages";
        }

        return normalized;
    }

    // Heuristic Seeding Engine
    private HydrationResult GenerateHeuristicHydration(Entity entity, HydrationOptions options, bool isSpanish, string promptUsed, WebResearchResult? webResearch, string? customPrompt)
    {
        var properties = new Dictionary<string, string>();
        string notes;
        int conf = 75;
        int comp = 80;
        var mode = (options.HydrationMode ?? "factual").Trim().ToLowerInvariant();
        var type = entity.EntityType?.Name ?? (isSpanish ? "Entidad general" : "General entity");
        var normalizedInstruction = NormalizeInstruction(customPrompt);

        if (isSpanish)
        {
            properties["tipoEntidad"] = type;
            switch (mode)
            {
                case "psychological":
                    properties["motivacionesProbables"] = "Busca reducir fricción interna y sostener coherencia con sus creencias dominantes.";
                    properties["miedosProbables"] = "Evita escenarios percibidos como pérdida de control, identidad o seguridad.";
                    properties["patronesDeComportamiento"] = "Tiende a repetir respuestas aprendidas bajo presión.";
                    notes = $"La entidad '{entity.Name}' puede entenderse mejor desde sus motivaciones, miedos y contradicciones internas. Conviene validar qué creencias activan sus decisiones y qué patrones aparecen cuando aumenta la tensión.";
                    break;
                case "organizational":
                    properties["rolOperativo"] = "Actor con impacto sobre flujos, coordinación o decisiones.";
                    properties["incentivos"] = "Busca sostener resultados, posición o autonomía dentro del sistema.";
                    properties["dependencias"] = "Su efectividad depende de información, alineación y capacidad de ejecución.";
                    notes = $"La entidad '{entity.Name}' debe analizarse por su rol, incentivos y dependencias dentro del sistema. Es útil revisar dónde crea bloqueos, dónde concentra poder y qué relaciones condicionan su desempeño.";
                    break;
                case "strategic":
                    properties["objetivoPrincipal"] = "Mejorar su posición o resultados dentro del escenario actual.";
                    properties["riesgosClave"] = "Puede amplificar cuellos de botella, conflicto o decisiones reactivas.";
                    properties["palancas"] = "Ajustes en prioridades, relaciones o recursos pueden cambiar su impacto sistémico.";
                    notes = $"La entidad '{entity.Name}' tiene relevancia estratégica por cómo afecta riesgos, restricciones y oportunidades. Conviene identificar qué palancas concretas modificarían su impacto en el universo.";
                    break;
                default:
                    properties["rasgosObservables"] = "Presenta características útiles para describir su función y estado actual.";
                    properties["funcionActual"] = "Actúa como nodo del sistema con efectos directos sobre otras entidades.";
                    properties["nivelDeImpacto"] = "Moderado";
                    notes = $"La entidad '{entity.Name}' puede describirse de forma más concreta usando atributos observables, una función clara y efectos identificables en el sistema.";
                    break;
            }
        }
        else
        {
            properties["entityType"] = type;
            switch (mode)
            {
                case "psychological":
                    properties["likelyMotivations"] = "Seeks to reduce inner friction and preserve coherence with dominant beliefs.";
                    properties["likelyFears"] = "Avoids situations perceived as loss of control, identity, or safety.";
                    properties["behavioralPatterns"] = "Tends to repeat learned responses under pressure.";
                    notes = $"The entity '{entity.Name}' is better understood through motivations, fears, and internal contradictions. Validate which beliefs drive its decisions and which patterns emerge under stress.";
                    break;
                case "organizational":
                    properties["operationalRole"] = "Actor with impact on flow, coordination, or decisions.";
                    properties["incentives"] = "Seeks to preserve results, position, or autonomy within the system.";
                    properties["dependencies"] = "Its effectiveness depends on information, alignment, and execution capacity.";
                    notes = $"The entity '{entity.Name}' should be analyzed through role, incentives, and dependencies inside the system. Review where it creates bottlenecks, where it concentrates power, and which relationships shape its performance.";
                    break;
                case "strategic":
                    properties["primaryGoal"] = "Improve its position or outcomes inside the current scenario.";
                    properties["keyRisks"] = "May amplify bottlenecks, conflict, or reactive decisions.";
                    properties["leveragePoints"] = "Adjustments in priorities, relationships, or resources may shift its systemic impact.";
                    notes = $"The entity '{entity.Name}' has strategic relevance because of how it affects risks, constraints, and opportunities. Identify which concrete leverage points would modify its impact in the universe.";
                    break;
                default:
                    properties["observableTraits"] = "Shows traits useful for describing its function and current state.";
                    properties["currentFunction"] = "Acts as a system node with direct effects on other entities.";
                    properties["impactLevel"] = "Moderate";
                    notes = $"The entity '{entity.Name}' can be described more concretely through observable attributes, a clear function, and identifiable effects on the system.";
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedInstruction))
        {
            ApplyInstructionDrivenProperties(properties, normalizedInstruction, isSpanish);
            notes = isSpanish
                ? $"La hidratación prioriza el foco solicitado por el usuario. {notes}"
                : $"The hydration prioritizes the user-requested focus. {notes}";
            conf = Math.Max(conf, 78);
            comp = Math.Max(comp, 84);
        }

        if (!string.IsNullOrWhiteSpace(webResearch?.Summary))
        {
            properties[isSpanish ? "contextoWebReciente" : "recentWebContext"] = webResearch.Summary;
            conf = 80;
            comp = 85;
        }

        return new HydrationResult
        {
            ConfidenceScore = conf,
            CompletenessScore = comp,
            SuggestedProperties = JsonSerializer.Serialize(properties),
            SuggestedPropertiesJson = JsonSerializer.Serialize(properties),
            PromptUsed = promptUsed,
            ProviderUsed = _providerName,
            Sources = webResearch?.Sources.Select(x => $"{x.Title}: {x.Url}").ToList() ?? new List<string>(),
            SuggestedNotes = notes,
            AnalysisNotes = notes
        };
    }

    private static string BuildHydrationPrompt(Entity entity, HydrationOptions options, string? customPrompt, WebResearchResult? webResearch, bool isSpanish)
    {
        var entityType = entity.EntityType?.Name ?? (isSpanish ? "General" : "General");
        var mode = (options.HydrationMode ?? "factual").Trim().ToLowerInvariant();
        var modeName = GetHydrationModeName(mode, isSpanish);
        var modeObjective = GetHydrationModeObjective(mode, isSpanish);
        var requestedFocus = BuildRequestedFocus(options, isSpanish);
        var researchBlock = BuildResearchBlock(webResearch, isSpanish);

        return isSpanish
            ? $"""
              TAREA:
              Hidrata la entidad '{entity.Name}' de tipo '{entityType}'.

              INSTRUCCIÓN PRIORITARIA DEL USUARIO:
              {(string.IsNullOrWhiteSpace(customPrompt) ? "No se proporcionó una instrucción adicional." : customPrompt.Trim())}

              MODO DE HIDRATACIÓN:
              {modeName}

              OBJETIVO DEL MODO:
              {modeObjective}

              CONTEXTO ACTUAL:
              - Nombre: {entity.Name}
              - Tipo: {entityType}
              - Descripción actual: {entity.Description}
              - Notas actuales: {entity.Notes}

              ENFOQUES PRIORITARIOS:
              {requestedFocus}

              REGLAS:
              - Responde SOLO en español.
              - La instrucción prioritaria del usuario tiene precedencia sobre los defaults genéricos.
              - Devuelve la respuesta como texto natural, claro y útil para poblar la entidad.
              - No uses JSON salvo que sea estrictamente necesario de forma espontánea.
              - Nunca repitas ni cites literalmente el prompt, las instrucciones ni los encabezados dentro de la respuesta.
              - No inventes hechos no sustentados; si infieres algo, hazlo de forma prudente.
              - Prioriza contenido concreto, reutilizable y útil para editar la entidad.
              - La salida ideal es un texto que pueda copiarse directamente a la descripción o notas de la entidad.

              {researchBlock}
              """
            : $"""
              TASK:
              Hydrate the entity '{entity.Name}' of type '{entityType}'.

              PRIORITY USER INSTRUCTION:
              {(string.IsNullOrWhiteSpace(customPrompt) ? "No additional instruction was provided." : customPrompt.Trim())}

              HYDRATION MODE:
              {modeName}

              MODE OBJECTIVE:
              {modeObjective}

              CURRENT CONTEXT:
              - Name: {entity.Name}
              - Type: {entityType}
              - Current description: {entity.Description}
              - Current notes: {entity.Notes}

              PRIORITY FOCUS:
              {requestedFocus}

              RULES:
              - Respond ONLY in English.
              - The priority user instruction overrides generic defaults and must not be ignored.
              - Return the answer as natural, clear, useful text for populating the entity.
              - Do not use JSON unless it appears naturally and is genuinely necessary.
              - Never repeat or quote the prompt, instructions, or section headers in the response.
              - Do not invent unsupported facts; if you infer, do it cautiously.
              - Prioritize concrete, reusable, and useful content for editing the entity.
              - The ideal output is text that can be copied directly into the entity description or notes.

              {researchBlock}
              """;
    }

    private static string BuildHydrationSystemPrompt(bool isSpanish)
    {
        return isSpanish
            ? "Eres un consultor experto en ontologías, modelado sistémico e investigación web. Responde en texto claro, directo y útil para rellenar la entidad. No repitas el prompt ni describas las instrucciones recibidas."
            : "You are an expert ontology, systems-mapping, and web-research consultant. Respond in clear, direct, useful text for filling the entity. Do not repeat the prompt or describe the instructions you received.";
    }

    private static string BuildPromptTranscript(string systemPrompt, string userPrompt)
    {
        return $"SYSTEM:{Environment.NewLine}{systemPrompt}{Environment.NewLine}{Environment.NewLine}USER:{Environment.NewLine}{userPrompt}";
    }

    private static string GetHydrationModeName(string mode, bool isSpanish)
    {
        return mode switch
        {
            "psychological" => isSpanish ? "Psicológico" : "Psychological",
            "organizational" => isSpanish ? "Organizacional" : "Organizational",
            "strategic" => isSpanish ? "Estratégico" : "Strategic",
            _ => isSpanish ? "Factual" : "Factual"
        };
    }

    private static string GetHydrationModeObjective(string mode, bool isSpanish)
    {
        return mode switch
        {
            "psychological" => isSpanish
                ? "Profundiza en motivaciones, miedos, creencias, contradicciones internas y patrones de comportamiento."
                : "Go deeper on motivations, fears, beliefs, internal contradictions, and behavioral patterns.",
            "organizational" => isSpanish
                ? "Describe rol, incentivos, dependencias, conflictos, poder, estructura y fricciones operativas."
                : "Describe role, incentives, dependencies, conflicts, power, structure, and operational friction.",
            "strategic" => isSpanish
                ? "Describe objetivos, riesgos, oportunidades, restricciones, palancas y efectos sistémicos."
                : "Describe goals, risks, opportunities, constraints, leverage points, and systemic effects.",
            _ => isSpanish
                ? "Describe hechos observables, rasgos concretos, función actual y atributos claros."
                : "Describe observable facts, concrete traits, current function, and clear attributes."
        };
    }

    private static string BuildRequestedFocus(HydrationOptions options, bool isSpanish)
    {
        var items = new List<string>();
        if (options.IncludePersonalities) items.Add(isSpanish ? "- personalidad o estilo dominante" : "- personality or dominant style");
        if (options.IncludeMotivations) items.Add(isSpanish ? "- motivaciones" : "- motivations");
        if (options.IncludeFears) items.Add(isSpanish ? "- miedos" : "- fears");
        if (options.IncludeIncentives) items.Add(isSpanish ? "- incentivos" : "- incentives");
        if (options.IncludeBehavioralPatterns) items.Add(isSpanish ? "- patrones de comportamiento" : "- behavioral patterns");

        if (items.Count == 0)
            items.Add(isSpanish ? "- atributos útiles y verificables" : "- useful and verifiable attributes");

        return string.Join(Environment.NewLine, items);
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

    private static string NormalizeInstruction(string? customPrompt)
    {
        if (string.IsNullOrWhiteSpace(customPrompt))
            return string.Empty;

        var singleLine = customPrompt
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return singleLine.Length <= 240
            ? singleLine
            : $"{singleLine[..240].Trim()}...";
    }

    private static void ApplyInstructionDrivenProperties(Dictionary<string, string> properties, string instruction, bool isSpanish)
    {
        var normalized = instruction.ToLowerInvariant();

        if (normalized.Contains("motiv"))
            properties[isSpanish ? "motivacionesSolicitadas" : "requestedMotivations"] = isSpanish
                ? "La instrucción pide profundizar en motivaciones y razones internas."
                : "The instruction asks to go deeper on motivations and internal drivers.";

        if (normalized.Contains("miedo") || normalized.Contains("fear"))
            properties[isSpanish ? "miedosSolicitados" : "requestedFears"] = isSpanish
                ? "La instrucción pide identificar miedos, resistencias o amenazas percibidas."
                : "The instruction asks to identify fears, resistance, or perceived threats.";

        if (normalized.Contains("riesgo") || normalized.Contains("risk"))
            properties[isSpanish ? "riesgosSolicitados" : "requestedRisks"] = isSpanish
                ? "La instrucción pide evaluar riesgos, vulnerabilidades o escenarios de pérdida."
                : "The instruction asks to assess risks, vulnerabilities, or downside scenarios.";

        if (normalized.Contains("rol") || normalized.Contains("role") || normalized.Contains("organiz"))
            properties[isSpanish ? "rolSolicitado" : "requestedRole"] = isSpanish
                ? "La instrucción pide mayor claridad sobre rol, función o contexto organizacional."
                : "The instruction asks for more clarity about role, function, or organizational context.";

        if (normalized.Contains("relaci") || normalized.Contains("relationship") || normalized.Contains("influ"))
            properties[isSpanish ? "relacionesSolicitadas" : "requestedRelationships"] = isSpanish
                ? "La instrucción pide observar vínculos, influencias o dependencias relevantes."
                : "The instruction asks to observe relevant links, influences, or dependencies.";

        if (normalized.Contains("web") || normalized.Contains("internet") || normalized.Contains("news") || normalized.Contains("noticia") || normalized.Contains("actual"))
            properties[isSpanish ? "investigacionSolicitada" : "requestedResearch"] = isSpanish
                ? "La instrucción requiere apoyo en investigación web o contexto reciente."
                : "The instruction requires support from web research or recent context.";
    }

    private static string? SanitizeHydrationText(string? value, string prompt, string? customPrompt)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (LooksLikePromptEcho(trimmed, prompt, customPrompt))
        {
            var extracted = ExtractMeaningfulHydrationText(trimmed);
            return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
        }

        return trimmed;
    }

    private static string SanitizeSuggestedProperties(JsonNode? node, string prompt, string? customPrompt)
    {
        if (node is not JsonObject obj)
            return string.Empty;

        var sanitized = new JsonObject();
        foreach (var item in obj)
        {
            if (item.Key is null || item.Value is null)
                continue;

            var normalizedKey = item.Key.Trim().ToLowerInvariant();
            if (normalizedKey is "prompt" or "promptused" or "instruction" or "userinstruction" or "requestedfocus" or "focosolicitado")
                continue;

            if (item.Value is JsonValue valueNode)
            {
                var scalar = valueNode.ToString();
                if (LooksLikePromptEcho(scalar, prompt, customPrompt))
                    continue;
            }

            sanitized[item.Key] = item.Value.DeepClone();
        }

        return sanitized.Count > 0
            ? sanitized.ToJsonString()
            : string.Empty;
    }

    private static bool LooksLikePromptEcho(string? value, string prompt, string? customPrompt)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalizedValue = NormalizeForComparison(value);
        if (string.IsNullOrWhiteSpace(normalizedValue))
            return false;

        var markers = new[]
        {
            "system",
            "user",
            "tarea",
            "task",
            "instruccion prioritaria del usuario",
            "priority user instruction",
            "modo de hidratacion",
            "hydration mode",
            "objetivo del modo",
            "mode objective",
            "contexto actual",
            "current context",
            "enfoques prioritarios",
            "priority focus",
            "reglas",
            "rules"
        };

        if (markers.Any(normalizedValue.Contains))
            return true;

        var normalizedPrompt = NormalizeForComparison(prompt);
        if (!string.IsNullOrWhiteSpace(normalizedPrompt) && normalizedValue.Length > 40 && normalizedPrompt.Contains(normalizedValue))
            return true;

        var normalizedInstruction = NormalizeForComparison(customPrompt);
        if (!string.IsNullOrWhiteSpace(normalizedInstruction)
            && normalizedInstruction.Length > 20
            && normalizedValue.Contains(normalizedInstruction))
            return true;

        return false;
    }

    private static string? ExtractMeaningfulHydrationText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        var lines = normalized
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x =>
                !string.IsNullOrWhiteSpace(x) &&
                !x.EndsWith(":") &&
                !x.Equals("TAREA", StringComparison.OrdinalIgnoreCase) &&
                !x.Equals("TASK", StringComparison.OrdinalIgnoreCase) &&
                !x.Equals("REGLAS", StringComparison.OrdinalIgnoreCase) &&
                !x.Equals("RULES", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("INSTRUCCIÓN PRIORITARIA DEL USUARIO", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("PRIORITY USER INSTRUCTION", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("MODO DE HIDRATACIÓN", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("HYDRATION MODE", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("OBJETIVO DEL MODO", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("MODE OBJECTIVE", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("CONTEXTO ACTUAL", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("CURRENT CONTEXT", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("ENFOQUES PRIORITARIOS", StringComparison.OrdinalIgnoreCase) &&
                !x.StartsWith("PRIORITY FOCUS", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (lines.Count == 0)
            return null;

        var combined = string.Join(" ", lines).Trim();
        return combined.Length < 20 ? null : combined;
    }

    private static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace(":", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("\"", " ", StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();
    }

    private HydrationResult? TryParseStructuredHydrationResponse(
        string rawResponse,
        string prompt,
        string? customPrompt,
        string promptUsed,
        WebResearchResult? webResearch,
        bool isSpanish)
    {
        var jsonPayload = ExtractJsonObject(rawResponse);
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return null;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(jsonPayload);
        }
        catch
        {
            return null;
        }

        if (node is not JsonObject)
            return null;

        var analysisNotes = SanitizeHydrationText(
            node["analysisNotes"]?.GetValue<string>()?.Trim(),
            prompt,
            customPrompt);
        var suggestedPropertiesJson = SanitizeSuggestedProperties(
            node["suggestedProperties"],
            prompt,
            customPrompt);

        if (string.IsNullOrWhiteSpace(analysisNotes) && string.IsNullOrWhiteSpace(suggestedPropertiesJson))
            return null;

        return new HydrationResult
        {
            ConfidenceScore = node["confidenceScore"]?.GetValue<int>() ?? 70,
            CompletenessScore = node["completenessScore"]?.GetValue<int>() ?? 75,
            SuggestedProperties = suggestedPropertiesJson,
            SuggestedPropertiesJson = suggestedPropertiesJson,
            PromptUsed = promptUsed,
            ProviderUsed = _providerName,
            Sources = webResearch?.Sources.Select(x => $"{x.Title}: {x.Url}").ToList() ?? new List<string>(),
            SuggestedNotes = analysisNotes ?? (isSpanish ? "Completado mediante IA." : "Completed via AI."),
            AnalysisNotes = analysisNotes ?? (isSpanish ? "Completado mediante IA." : "Completed via AI.")
        };
    }

    private HydrationResult? BuildPlainTextHydrationResult(
        string rawResponse,
        string prompt,
        string? customPrompt,
        string promptUsed,
        WebResearchResult? webResearch,
        bool isSpanish)
    {
        var cleaned = SanitizeHydrationText(rawResponse, prompt, customPrompt);
        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        return new HydrationResult
        {
            ConfidenceScore = 70,
            CompletenessScore = 75,
            SuggestedProperties = string.Empty,
            SuggestedPropertiesJson = string.Empty,
            PromptUsed = promptUsed,
            ProviderUsed = _providerName,
            Sources = webResearch?.Sources.Select(x => $"{x.Title}: {x.Url}").ToList() ?? new List<string>(),
            SuggestedNotes = cleaned,
            AnalysisNotes = cleaned
        };
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

    private sealed record ProviderConfiguration(string Provider, string Endpoint, string? Model, string? ApiKey, bool IsConfigured);
    private sealed record OllamaConfiguration(string Endpoint, string? Model, string? ApiKey, bool IsConfigured);
}
