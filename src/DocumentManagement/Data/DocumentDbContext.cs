using DocumentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Data;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Document>();
        e.HasKey(x => x.Id);
        e.Property(x => x.Title).HasMaxLength(200).IsRequired();
        e.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        e.Property(x => x.StoredFileName).HasMaxLength(100).IsRequired();
        e.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.CreatedAtUtc);
        e.HasIndex(x => x.Category);
        e.HasIndex(x => x.IsDeleted);
    }
}
