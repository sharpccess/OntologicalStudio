using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using System.Text.Json;

namespace OntologicalStudio.Infrastructure.Services;

public class LibraryCatalogService : ILibraryCatalogService
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly string _catalogPath;

    public LibraryCatalogService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OntologicalStudio");
        Directory.CreateDirectory(root);
        _catalogPath = Path.Combine(root, "library-catalog.json");
    }

    public async Task<IReadOnlyList<EntityLibraryItem>> GetEntityItemsAsync()
    {
        var catalog = await LoadAsync();
        return catalog.Entities
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<UniverseModelLibraryItem>> GetUniverseModelItemsAsync()
    {
        var catalog = await LoadAsync();
        return catalog.UniverseModels
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<EntityLibraryItem> SaveEntityAsync(EntityLibraryItem item)
    {
        await _sync.WaitAsync();
        try
        {
            var catalog = await LoadInternalAsync();
            var existing = catalog.Entities.FirstOrDefault(x => x.Id == item.Id);
            if (existing is null)
            {
                item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
                item.SavedAtUtc = DateTime.UtcNow;
                catalog.Entities.RemoveAll(x => string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.EntityTypeName, item.EntityTypeName, StringComparison.OrdinalIgnoreCase));
                catalog.Entities.Add(item);
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.EntityTypeName = item.EntityTypeName;
                existing.Notes = item.Notes;
                existing.HydrationData = item.HydrationData;
                existing.ConfidenceLevel = item.ConfidenceLevel;
                existing.CompletenessScore = item.CompletenessScore;
                existing.Properties = item.Properties;
                existing.SavedAtUtc = DateTime.UtcNow;
            }

            await SaveInternalAsync(catalog);
            return item;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<UniverseModelLibraryItem> SaveUniverseModelAsync(UniverseModelLibraryItem item)
    {
        await _sync.WaitAsync();
        try
        {
            var catalog = await LoadInternalAsync();
            var existing = catalog.UniverseModels.FirstOrDefault(x => x.Id == item.Id);
            if (existing is null)
            {
                item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
                item.SavedAtUtc = DateTime.UtcNow;
                catalog.UniverseModels.RemoveAll(x => string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                catalog.UniverseModels.Add(item);
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.Entities = item.Entities;
                existing.Relationships = item.Relationships;
                existing.SavedAtUtc = DateTime.UtcNow;
            }

            await SaveInternalAsync(catalog);
            return item;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task DeleteEntityAsync(Guid id)
    {
        await _sync.WaitAsync();
        try
        {
            var catalog = await LoadInternalAsync();
            catalog.Entities.RemoveAll(x => x.Id == id);
            await SaveInternalAsync(catalog);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task DeleteUniverseModelAsync(Guid id)
    {
        await _sync.WaitAsync();
        try
        {
            var catalog = await LoadInternalAsync();
            catalog.UniverseModels.RemoveAll(x => x.Id == id);
            await SaveInternalAsync(catalog);
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<LibraryCatalog> LoadAsync()
    {
        await _sync.WaitAsync();
        try
        {
            return await LoadInternalAsync();
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<LibraryCatalog> LoadInternalAsync()
    {
        if (!File.Exists(_catalogPath))
            return new LibraryCatalog();

        try
        {
            var json = await File.ReadAllTextAsync(_catalogPath);
            return JsonSerializer.Deserialize<LibraryCatalog>(json) ?? new LibraryCatalog();
        }
        catch
        {
            return new LibraryCatalog();
        }
    }

    private async Task SaveInternalAsync(LibraryCatalog catalog)
    {
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_catalogPath, json);
    }
}