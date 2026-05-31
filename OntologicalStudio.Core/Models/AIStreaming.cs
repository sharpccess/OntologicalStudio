namespace OntologicalStudio.Core.Models;

public class AIRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "text";
    public bool JsonMode { get; set; }
    public float Temperature { get; set; } = 0.7f;
}

public abstract record AIChunk;

public record TextChunk(string Text) : AIChunk;

public record ImageChunk(byte[] Bytes, string MimeType) : AIChunk;

public record FileChunk(string FileName, byte[] Bytes, string MimeType) : AIChunk;

public record DoneChunk(int InputTokens, int OutputTokens) : AIChunk;