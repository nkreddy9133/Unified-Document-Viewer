using Microsoft.EntityFrameworkCore;

namespace UnifiedDocumentViewer.Data;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<SearchAttempt> SearchAttempts => Set<SearchAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SearchAttempt>(entity =>
        {
            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Vin)
                .HasMaxLength(17)
                .IsRequired();
            entity.Property(attempt => attempt.Status)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(attempt => attempt.ErrorMessage)
                .HasMaxLength(500);
            entity.HasIndex(attempt => attempt.Vin);
            entity.HasIndex(attempt => attempt.SearchedAtUtc);
        });
    }
}
