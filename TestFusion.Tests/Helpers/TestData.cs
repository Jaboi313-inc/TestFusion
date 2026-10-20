using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Models;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.Data;

namespace TestFusion.Tests.Helpers;

internal static class TestData
{
    public static AppDbContext CreateInMemoryContext(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static (AppDbContext Context, SqliteConnection Connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqliteTestAppDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }

    public static TestReportModel CreateReport(string id, string partNumber = "PN-1", DateTimeOffset? time = null)
    {
        var result = CreateResult(id, partNumber, time);

        return new TestReportModel
        {
            Id = result.Id,
            TimeOffTesting = result.TimeOffTesting,
            PartNumber = result.PartNumber,
            PartBrand = result.PartBrand,
            PartType = result.PartType,
            CustomerName = result.CustomerName,
            CustomerPhone = result.CustomerPhone,
            CustomerMail = result.CustomerMail,
            CustomerNotes = result.CustomerNotes,
            TestNotes = result.TestNotes,
            InjectorSerialNumber = result.InjectorSerialNumber,
            Json = JsonSerializer.Serialize(result)
        };
    }

    public static TestResultModel CreateResult(string id, string partNumber = "PN-1", DateTimeOffset? time = null)
    {
        return new TestResultModel
        {
            Id = id,
            TimeOffTesting = time ?? new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero),
            PartNumber = partNumber,
            PartBrand = "Bosch",
            PartType = "Injector",
            CustomerName = "Test Customer",
            CustomerPhone = "0612345678",
            CustomerMail = "test@example.com",
            CustomerNotes = "Customer note",
            TestNotes = "Test note",
            InjectorSerialNumber = $"SN-{id}",
            Tests =
            [
                new TestModel
                {
                    TestId = 10,
                    TestName = "Leak test",
                    TestStatus = 4,
                    TestOrder = 1,
                    TestTime = 15,
                    TestType = "Flow",
                    TestResponseTime = 42,
                    SubTests =
                    [
                        new SubTestModel
                        {
                            TankName = "Tank 1",
                            TankPosition = 1,
                            Min = 1,
                            Max = 5,
                            BlueMin = 0,
                            BlueMax = 6,
                            YellowMin = 0.5m,
                            YellowMax = 5.5m,
                            TolBlue = 1,
                            ProcentMin = "-10%",
                            ProcentMax = "+10%",
                            ProcentText = "OK",
                            ResultMin = "OK",
                            ResultAverage = "OK",
                            ResultMax = "OK",
                            ResultColor = 6,
                            Results = [2, 3, 4]
                        }
                    ]
                }
            ]
        };
    }

    public static GeneratedModel CreateGeneratedModel()
    {
        var result = CreateResult("report-1");
        var test = result.Tests[0];

        return new GeneratedModel
        {
            SelectedIds = [result.Id],
            AllTests = [test],
            Injectors =
            [
                new TestResultViewModel
                {
                    Data = result,
                    NormalizedTests =
                    [
                        new TestCellModel
                        {
                            TestId = test.TestId,
                            TestName = test.TestName,
                            Exists = true,
                            IsSkipped = false,
                            Status = test.TestStatus,
                            Time = test.TestTime,
                            Response = test.TestResponseTime.ToString(),
                            SubTests = test.SubTests
                        }
                    ]
                }
            ]
        };
    }

    public static string CreateSourceJson(string id, bool skipped = false)
    {
        if (skipped)
        {
            return $$"""
            {
              "_id": "{{id}}",
              "datetime": "18/10/2026 15:00:00",
              "actuator_code": "PN-1",
              "actuator_Brand": "Bosch",
              "actuator_type": "Injector",
              "customer_name": "Customer",
              "customer_phone": "0612345678",
              "customer_mail": "customer@example.com",
              "customer_notes": "Note",
              "notes": "Test note",
              "SlotsData": [{ "sn": "SN-123" }],
              "TestsDataPages": [
                {
                  "TestData": [
                    {
                      "TestData": {
                        "status": 1,
                        "test_id": 5,
                        "test_name": "Leak test : SKIPPED",
                        "test_order": 2
                      }
                    }
                  ]
                }
              ]
            }
            """;
        }

        return $$"""
        {
          "_id": "{{id}}",
          "datetime": "18/10/2026 15:00:00",
          "actuator_code": "PN-1",
          "actuator_Brand": "Bosch",
          "actuator_type": "Injector",
          "customer_name": "Customer",
          "customer_phone": "0612345678",
          "customer_mail": "customer@example.com",
          "customer_notes": "Note",
          "notes": "Test note",
          "SlotsData": [{ "sn": "SN-123" }],
          "TestsDataPages": [
            {
              "TestData": [
                {
                  "TestData": {
                    "status": 4,
                    "test_id": 5,
                    "test_name": "Leak test",
                    "test_order": 2
                  },
                  "test_time": "15",
                  "RspResults": {
                    "type": "Flow",
                    "dataArray": [{ "result": "42" }]
                  },
                  "TestLimits": [
                    {
                      "tank_name": "Tank 1",
                      "tank_position": 1,
                      "min_green": "1.5",
                      "max_green": "5.5",
                      "min_blue": "0.5",
                      "max_blue": "6.5",
                      "min_yellow": "1.0",
                      "max_yellow": "6.0",
                      "tol_blue": "1.25",
                      "max_green_label": "+10%",
                      "min_green_label": "-10%",
                      "text_green": "OK"
                    }
                  ],
                  "TestResults": [
                    {
                      "tank_position": "0",
                      "postion": 1,
                      "AvrResult": "OK",
                      "MinResult": "LOW",
                      "MaxResult": "HIGH",
                      "result_color": "6",
                      "results": "1.1~2.2~bad~3.3"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;
    }
    private sealed class SqliteTestAppDbContext : AppDbContext
    {
        public SqliteTestAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestReportModel>()
                .Property(x => x.TimeOffTesting)
                .HasColumnType("TEXT");
        }
    }

}
