using Microsoft.EntityFrameworkCore;
using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EntityType> EntityTypes { get; set; }
    public DbSet<Entity> Entities { get; set; }
    public DbSet<RelationshipType> RelationshipTypes { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<Universe> Universes { get; set; }
    public DbSet<Scenario> Scenarios { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<EntityScenario> EntityScenarios { get; set; }
    public DbSet<EntityTag> EntityTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEntityType(modelBuilder);
        ConfigureEntity(modelBuilder);
        ConfigureRelationshipType(modelBuilder);
        ConfigureRelationship(modelBuilder);
        ConfigureUniverse(modelBuilder);
        ConfigureScenario(modelBuilder);
        ConfigureTag(modelBuilder);
        ConfigureEntityScenario(modelBuilder);
        ConfigureEntityTag(modelBuilder);
    }

    private void ConfigureEntityType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityType>(entity =>
        {
            entity.ToTable("EntityType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.SuggestedHydrationFields).HasColumnType("TEXT");
            entity.Property(e => e.IsDefaultTemplate).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private void ConfigureEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity>(entity =>
        {
            entity.ToTable("Entity");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.Properties).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.Notes).HasColumnType("TEXT");
            entity.Property(e => e.HydrationData).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.ConfidenceLevel).HasDefaultValue(0);
            entity.Property(e => e.CompletenessScore).HasDefaultValue(0);
            entity.Property(e => e.PositionX).HasDefaultValue(0.0);
            entity.Property(e => e.PositionY).HasDefaultValue(0.0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(e => e.EntityType)
                .WithMany(t => t.Entities)
                .HasForeignKey(e => e.EntityTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Universe)
                .WithMany(u => u.Entities)
                .HasForeignKey(e => e.UniverseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRelationshipType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RelationshipType>(entity =>
        {
            entity.ToTable("RelationshipType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.AllowedSourceTypes).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.AllowedTargetTypes).HasColumnType("TEXT").HasDefaultValue("[]");
            entity.Property(e => e.Bidirectional).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private void ConfigureRelationship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.ToTable("Relationship");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Properties).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(r => r.SourceEntity)
                .WithMany(e => e.SourceRelationships)
                .HasForeignKey(r => r.SourceEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.TargetEntity)
                .WithMany(e => e.TargetRelationships)
                .HasForeignKey(r => r.TargetEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.RelationshipType)
                .WithMany(t => t.Relationships)
                .HasForeignKey(r => r.RelationshipTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUniverse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Universe>(entity =>
        {
            entity.ToTable("Universe");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.Metadata).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private void ConfigureScenario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.ToTable("Scenario");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.Context).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.Goals).HasColumnType("TEXT");
            entity.Property(e => e.Results).HasColumnType("TEXT").HasDefaultValue("{}");
            entity.Property(e => e.Status).HasDefaultValue(ScenarioStatus.Draft);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(s => s.Universe)
                .WithMany(u => u.Scenarios)
                .HasForeignKey(s => s.UniverseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tag");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasColumnType("TEXT");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private void ConfigureEntityScenario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityScenario>(entity =>
        {
            entity.HasKey(es => new { es.EntityId, es.ScenarioId });
            entity.Property(es => es.EntityId).HasColumnType("TEXT");
            entity.Property(es => es.ScenarioId).HasColumnType("TEXT");
            entity.Property(es => es.Role).HasColumnType("TEXT");
        });
    }

    private void ConfigureEntityTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityTag>(entity =>
        {
            entity.HasKey(et => new { et.EntityId, et.TagId });
            entity.Property(et => et.EntityId).HasColumnType("TEXT");
            entity.Property(et => et.TagId).HasColumnType("TEXT");
        });
    }
}
