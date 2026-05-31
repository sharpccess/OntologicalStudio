namespace OntologicalStudio.Core.Models;

public class SolutionArtifact : EntityBase
{
    public Guid SolutionId { get; set; }
    public Solution? Solution { get; set; }

    public ArtifactKind Kind { get; set; } = ArtifactKind.Text;

    /// <summary>e.g. text/plain, text/markdown, image/png, application/pdf.</summary>
    public string MimeType { get; set; } = "text/plain";

    /// <summary>For text/markdown/json: full content stored inline. Empty for binary artifacts.</summary>
    public string InlineContent { get; set; } = string.Empty;

    /// <summary>For binary artifacts: relative path inside the blob store.</summary>
    public string? BlobPath { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Display order within the solution.</summary>
    public int Order { get; set; }

    /// <summary>Optional friendly name for the artifact (filename, label, etc.).</summary>
    public string Label { get; set; } = string.Empty;
}
