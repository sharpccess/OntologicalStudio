namespace OntologicalStudio.Core.Interfaces;

/// <summary>
/// Persists binary or large artifacts (images, files, etc.) outside the database.
/// Returns a relative path that can be resolved later.
/// </summary>
public interface IBlobStore
{
    Task<BlobInfo> WriteAsync(byte[] bytes, string fileExtension, CancellationToken ct = default);
    Task<byte[]> ReadAsync(string relativePath, CancellationToken ct = default);
    string GetAbsolutePath(string relativePath);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}

public record BlobInfo(string RelativePath, long SizeBytes);
