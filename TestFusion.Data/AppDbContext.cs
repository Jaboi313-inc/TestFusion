using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Models;

namespace TestFusion.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestListItemModel> TestItems
        => Set<TestListItemModel>();

    public DbSet<StoredJsonModel> StoredJsons
        => Set<StoredJsonModel>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestListItemModel>(entity =>
        {
            entity.ToTable("test_items");

            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<StoredJsonModel>(entity =>
        {
            entity.ToTable("stored_jsons");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Json)
                .HasColumnType("TEXT");
        });
    }
}