using System.Net.Http;
using System.Text;
using System.Text.Json;
using OntologicalStudio.Application.Services;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Infrastructure.Services;

public class WebResearchService : IWebResearchService
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(20) };

    public async Task<WebResearchResult?> ResearchAsync(string query, string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var encoded = Uri.EscapeDataString(query);
        var url = $"https://api.duckduckgo.com/?q={encoded}&format=json&no_html=1&skip_disambig=1";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var result = new WebResearchResult
            {
                LanguageCode = languageCode,
                Query = query
            };

            var root = doc.RootElement;
            var sb = new StringBuilder();

            if (root.TryGetProperty("AbstractText", out var abstractText) && abstractText.ValueKind == JsonValueKind.String)
            {
                var text = abstractText.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    sb.AppendLine(text);
            }

            if (root.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in topics.EnumerateArray().Take(5))
                {
                    if (item.TryGetProperty("Text", out var textProp) && item.TryGetProperty("FirstURL", out var urlProp))
                    {
                        var text = textProp.GetString() ?? string.Empty;
                        var sourceUrl = urlProp.GetString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.Sources.Add(new WebResearchSource
                            {
                                Title = text.Split('-').FirstOrDefault()?.Trim() ?? text,
                                Url = sourceUrl,
                                Snippet = text
                            });
                            sb.AppendLine($"- {text}");
                        }
                    }
                    else if (item.TryGetProperty("Topics", out var nested) && nested.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var nestedItem in nested.EnumerateArray().Take(3))
                        {
                            if (!nestedItem.TryGetProperty("Text", out var nestedTextProp) ||
                                !nestedItem.TryGetProperty("FirstURL", out var nestedUrlProp))
                                continue;

                            var text = nestedTextProp.GetString() ?? string.Empty;
                            var sourceUrl = nestedUrlProp.GetString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(text))
                                continue;

                            result.Sources.Add(new WebResearchSource
                            {
                                Title = text.Split('-').FirstOrDefault()?.Trim() ?? text,
                                Url = sourceUrl,
                                Snippet = text
                            });
                            sb.AppendLine($"- {text}");
                        }
                    }
                }
            }

            result.Summary = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(result.Summary) && result.Sources.Count == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }
}