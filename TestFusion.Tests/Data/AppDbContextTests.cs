using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Models;
using TestFusion.Data;

namespace TestFusion.Tests.Data;

public class AppDbContextTests
{
    [Fact]
    public void Model_MapsTestReportToExpectedTableAndColumns()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(typeof(TestReportModel));

        Assert.NotNull(entity);
        Assert.Equal("test_reports", entity.GetTableName());

        var jsonProperty = entity.FindProperty(nameof(TestReportModel.Json));
        var timeProperty = entity.FindProperty(nameof(TestReportModel.TimeOffTesting));

        Assert.NotNull(jsonProperty);
        Assert.NotNull(timeProperty);

        Assert.Equal("TEXT", jsonProperty.GetColumnType());
        Assert.Equal("timestamp with time zone", timeProperty.GetColumnType());
    }

    [Fact]
    public void Model_HasExpectedIndexes()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(typeof(TestReportModel));

        Assert.NotNull(entity);

        var indexedProperties = entity.GetIndexes()
            .SelectMany(index => index.Properties)
            .Select(property => property.Name)
            .ToHashSet();

        Assert.Contains(nameof(TestReportModel.PartNumber), indexedProperties);
        Assert.Contains(nameof(TestReportModel.PartBrand), indexedProperties);
        Assert.Contains(nameof(TestReportModel.PartType), indexedProperties);
        Assert.Contains(nameof(TestReportModel.TimeOffTesting), indexedProperties);
        Assert.Contains(nameof(TestReportModel.CustomerName), indexedProperties);
        Assert.Contains(nameof(TestReportModel.CustomerMail), indexedProperties);
        Assert.Contains(nameof(TestReportModel.InjectorSerialNumber), indexedProperties);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=testfusion_tests;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}