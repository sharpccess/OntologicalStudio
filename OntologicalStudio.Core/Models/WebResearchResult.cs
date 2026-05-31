namespace OntologicalStudio.Core.Models;

public class WebResearchResult
{
    public string LanguageCode { get; set; } = "en";
    public string Query { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<WebResearchSource> Sources { get; set; } = new();
}

public class WebResearchSource
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
}