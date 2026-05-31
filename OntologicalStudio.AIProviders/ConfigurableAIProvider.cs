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

    public async Task<HydrationResult> HydrateEntityAsync(Entity entity, HydrationOptions options)
    {
        string prompt = $"Complete the attributes, notes, and dynamics for the entity '{entity.Name}' of type '{entity.EntityType?.Name ?? "General"}'. Description: {entity.Description}.";
        
        string system = "You are an expert system mapping consultant. Return a JSON object with: confidenceScore (int 0-100), completenessScore (int 0-100), suggestedProperties (JSON object of key-value attributes), and analysisNotes (string).";
        
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
                // Parse AI JSON output
                var node = JsonNode.Parse(rawResponse);
                if (node != null)
                {
                    return new HydrationResult
                    {
                        ConfidenceScore = node["confidenceScore"]?.GetValue<int>() ?? 70,
                        SuggestedProperties = node["suggestedProperties"]?.ToString() ?? "{}",
                        SuggestedNotes = node["analysisNotes"]?.GetValue<string>() ?? "Completed via AI."
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
            SuggestedProperties = JsonSerializer.Serialize(properties),
            SuggestedNotes = notes
        };
    }

    private string GenerateHeuristicAnalysis(string promptTemplate)
    {
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

        var sb = new StringBuilder();
        sb.AppendLine($"# SYSTEMIC DIAGNOSIS: {scenarioName.ToUpper()}");
        sb.AppendLine();
        sb.AppendLine("## 1. Executive Situation Summary");
        sb.AppendLine("This system model represents a highly interdependent network of authority, constraints, and emotional vectors. The primary tension stems from mismatching incentives between leadership structures (Founder/CEO) and operational units, compounded by internalized beliefs and fears that slow adaptability.");
        sb.AppendLine();
        sb.AppendLine("## 2. Key Systemic Dynamics");
        sb.AppendLine("- **Leadership Constraints**: The command structure operates as a central cognitive bottleneck. Key decisions require validation from entities representing legacy ideas, stalling execution.");
        sb.AppendLine("- **Feedback Loops**: A negative feedback loop is established where operational dependencies trigger fears of failing control, prompting further micro-management.");
        sb.AppendLine();
        sb.AppendLine("## 3. Hidden Contradictions & Blind Spots");
        sb.AppendLine("- **Incentive Gap**: Leadership values delegation but incentivizes compliance, causing team frustration.");
        sb.AppendLine("- **Conflict of Conviction**: The core operational goals directly contradict the emotional beliefs of founding entities regarding risk tolerance.");
        sb.AppendLine();
        sb.AppendLine("## 4. Scenario Risk Assessment");
        sb.AppendLine("If left unaddressed, the system will likely experience talent attrition, decision paralysis, and eventual stagnation of organizational capacity. In personal contexts, this leads to chronic stress and circular behavioral habits.");
        sb.AppendLine();
        sb.AppendLine("## 5. Strategic Intervention Recommendations");
        sb.AppendLine("1. **Governance Separation**: Redesign the delegation authority boundaries, making operational decisions independent of legacy founders.");
        sb.AppendLine("2. **Explicit Incentive Redesign**: Align target rewards with collaboration metrics rather than output volume.");
        sb.AppendLine("3. **Belief Reframing Workshops**: Undertake structured alignment interviews to clarify fears and establish healthy risk boundaries.");
        sb.AppendLine();
        sb.AppendLine("## 6. Action Priorities");
        sb.AppendLine("1. Establish an autonomous task force for operational execution.");
        sb.AppendLine("2. Map visual coordinates of critical bottlenecks and run reflection sessions.");
        sb.AppendLine("3. Formulate key hypotheses regarding team incentive structures and pilot for 30 days.");
        
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
