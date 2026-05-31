using OntologicalStudio.Core.Interfaces;

namespace OntologicalStudio.Infrastructure.Storage;

public class FileSystemBlobStore : IBlobStore
{
    private readonly string _root;

    public FileSystemBlobStore(string rootDirectory)
    {
        _root = rootDirectory;
        Directory.CreateDirectory(_root);
    }

    public async Task<BlobInfo> WriteAsync(byte[] bytes, string fileExtension, CancellationToken ct = default)
    {
        var ext = string.IsNullOrWhiteSpace(fileExtension) ? "bin" : fileExtension.TrimStart('.');
        var name = $"{Guid.NewGuid():N}.{ext}";
        var sub = name[..2];
        var dir = Path.Combine(_root, sub);
        Directory.CreateDirectory(dir);
        var rel = Path.Combine(sub, name).Replace('\\', '/');
        var abs = Path.Combine(_root, sub, name);
        await File.WriteAllBytesAsync(abs, bytes, ct);
        return new BlobInfo(rel, bytes.LongLength);
    }

    public Task<byte[]> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var abs = GetAbsolutePath(relativePath);
        return File.ReadAllBytesAsync(abs, ct);
    }

    public string GetAbsolutePath(string relativePath)
    {
        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_root, safe);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var abs = GetAbsolutePath(relativePath);
        if (File.Exists(abs)) File.Delete(abs);
        return Task.CompletedTask;
    }
}
