using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using OntologicalStudio.Persistence.Context;

namespace OntologicalStudio.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Tag> _tags;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
        _tags = context.Set<Tag>();
    }

    public async Task<Tag> GetByIdAsync(Guid id)
    {
        return await _tags.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _tags
            .Where(t => !t.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(Tag tag)
    {
        if (tag.Id == Guid.Empty)
            tag.Id = Guid.NewGuid();
        tag.CreatedAt = DateTime.UtcNow;
        await _tags.AddAsync(tag);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tag tag)
    {
        tag.UpdatedAt = DateTime.UtcNow;
        _tags.Update(tag);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Tag tag)
    {
        tag.IsDeleted = true;
        tag.UpdatedAt = DateTime.UtcNow;
        _tags.Update(tag);
        await _context.SaveChangesAsync();
    }
}
