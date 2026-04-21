using Microsoft.EntityFrameworkCore;

namespace ksse.ReadingProgress;

internal sealed class ProgressDbContext : DbContext
{
    public const string Id = "progress";

    public required DbSet<ProgressDocument> ProgressDocuments { get; init; }

    public ProgressDbContext(DbContextOptions<ProgressDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Id);
        modelBuilder.Entity<ProgressDocument>()
            .HasKey(p => new { p.User, p.Hash });
        base.OnModelCreating(modelBuilder);
    }
}
