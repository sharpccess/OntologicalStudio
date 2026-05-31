namespace OntologicalStudio.Core.Models;

public class AiConnectionSettings
{
    public string Provider { get; set; } = "ollama";
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OllamaEndpoint { get; set; } = string.Empty;
    public string OllamaApiKey { get; set; } = string.Empty;
    public string OllamaModel { get; set; } = string.Empty;
}