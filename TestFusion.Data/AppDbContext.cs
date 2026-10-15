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

    public DbSet<TestReportModel> TestReports
        => Set<TestReportModel>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestReportModel>(entity =>
        {
            entity.ToTable("test_reports");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Json)
                .HasColumnType("TEXT");

            entity.Property(x => x.TimeOffTesting)
                .HasColumnType("timestamp with time zone");


            entity.HasIndex(x => x.PartNumber);

            entity.HasIndex(x => x.PartBrand);

            entity.HasIndex(x => x.PartType);

            entity.HasIndex(x => x.TimeOffTesting);

            entity.HasIndex(x => x.CustomerName);

            entity.HasIndex(x => x.CustomerMail);

            entity.HasIndex(x => x.InjectorSerialNumber);
        });
    }
}