using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using TestFusion.Core.Helpers;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService.Services
{
    public class JSONService
    {
        public string ConvertToJson<T>(T model, bool prettyJson = false, bool useUnicodeSymbols = true)
        {
            return JsonSerializer.Serialize(
        model,
        new JsonSerializerOptions
        {
            WriteIndented = prettyJson,
            Encoder = useUnicodeSymbols
                ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                : JavaScriptEncoder.Default
        });
        }

        public TestListItemModel ConvertToTestListModel(TestResultModel testResultModel)
        {
            return new TestListItemModel
            {
                Id = testResultModel.Id,
                PartNumber = testResultModel.PartNumber,
                PartBrand = testResultModel.PartBrand,
                PartType = testResultModel.PartType,
                DateTime = testResultModel.TimeOffTesting
            };
        }

        // Helper functions
        private static string GetString(JsonElement root, string propertyName)
        {
            if (root.ValueKind == JsonValueKind.Null ||
                root.ValueKind == JsonValueKind.Undefined)
                return string.Empty;

            if (!root.TryGetProperty(propertyName, out var value))
                return string.Empty;

            if (value.ValueKind == JsonValueKind.Null ||
                value.ValueKind == JsonValueKind.Undefined)
                return string.Empty;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => string.Empty
            };
        }

        private static DateTimeOffset GetDateTimeOffset(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return DateTimeOffset.MinValue;

            return DateTimeOffset.TryParse(value.GetString(), out var result)
                ? result.ToUniversalTime()
                : DateTimeOffset.MinValue;
        }

        private static int GetInt(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var intValue)
                ? intValue
                : 0;
        }

        private static string GetFirstResult(JsonElement rspResults)
        {
            if (rspResults.ValueKind != JsonValueKind.Object)
                return null;

            if (!rspResults.TryGetProperty("dataArray", out var arr))
                return null;

            foreach (var item in arr.EnumerateArray())
            {
                if (item.TryGetProperty("result", out var r))
                    return r.GetString() ?? null;
            }

            return null;
        }

        private static List<decimal> ParseResults(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<decimal>();

            return input
                .Split('~', StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    if (decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                        return (decimal?)v;

                    return null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
        }
        // --------------------------------------------------------------------------------------


        public TestResultModel ConvertToTestResultModel(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var testResultModel = new TestResultModel
            {
                Id = GetString(root, "_id"),
                TimeOffTesting = GetDateTimeOffset(root, "datetime"),

                PartNumber = GetString(root, "actuator_code"),
                PartBrand = GetString(root, "actuator_Brand"),
                PartType = GetString(root, "actuator_type"),

                CustomerName = GetString(root, "customer_name"),
                CustomerPhone = GetString(root, "customer_phone"),
                CustomerMail = GetString(root, "customer_mail"),
                CustomerNotes = GetString(root, "customer_notes"),

                TestNotes = GetString(root, "notes"),

                Tests = new List<TestModel>()
            };

            var pages = root.GetProperty("TestsDataPages");

            foreach (var page in pages.EnumerateArray())
            {
                if (!page.TryGetProperty("TestData", out var testDataArray))
                    continue;

                foreach (var item in testDataArray.EnumerateArray())
                {
                    item.TryGetProperty("TestData", out var testData);

                    if (GetInt(testData, "status") == 1)
                    {
                        var alteredTest = new TestModel
                        {
                            TestId = GetInt(testData, "test_id"),
                            TestName = GetString(testData, "test_name"),
                            TestStatus = GetInt(testData, "status"),
                            TestTime = 0,

                            TestType = "",
                            TestResponseTime = 0,

                            SubTests = new List<SubTestModel>()
                        };

                        testResultModel.Tests.Add(alteredTest);

                        continue;
                    }

                    item.TryGetProperty("RspResults", out var rspResults);

                    var test = new TestModel
                    {
                        TestId = GetInt(testData, "test_id"),
                        TestName = GetString(testData, "test_name"),
                        TestStatus = GetInt(testData, "status"),
                        TestTime = FromStringHelper.ToInt(GetString(item, "test_time")),
                        TestType = GetString(rspResults, "type"),
                        TestResponseTime = FromStringHelper.ToInt(GetFirstResult(rspResults)),

                        SubTests = new List<SubTestModel>()
                    };                    

                    testResultModel.Tests.Add(test);

                    item.TryGetProperty("TestLimits", out var limits);
                    item.TryGetProperty("TestResults", out var results);

                    var resultLookup = new Dictionary<string, JsonElement>();

                    if (results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in results.EnumerateArray())
                        {
                            var run = FromStringHelper.ToInt(GetString(r, "tank_position"));
                            var sub = GetInt(r, "postion");

                            var key = $"{run}_{sub}";
                            resultLookup[key] = r;
                        }
                    }

                    foreach (var limit in limits.EnumerateArray())
                    {
                        int subIndex = GetInt(limit, "tank_position");

                        int runIndex = (subIndex - 1) * 4;

                        var key = $"{runIndex}_{subIndex}";

                        resultLookup.TryGetValue(key, out var res);

                        var sub = new SubTestModel
                        {
                            TankName = GetString(limit, "tank_name"),
                            TankPosition = GetInt(limit, "tank_position"),

                            Min = FromStringHelper.ToDecimal(GetString(limit, "min_green")),
                            Max = FromStringHelper.ToDecimal(GetString(limit, "max_green")),

                            BlueMin = FromStringHelper.ToDecimal(GetString(limit, "min_blue")),
                            BlueMax = FromStringHelper.ToDecimal(GetString(limit, "max_blue")),
                            YellowMin = FromStringHelper.ToDecimal(GetString(limit, "min_yellow")),
                            YellowMax = FromStringHelper.ToDecimal(GetString(limit, "max_yellow")),

                            TolBlue = FromStringHelper.ToDecimal(GetString(limit, "tol_blue")),

                            ProcentMax = GetString(limit, "max_green_label"),
                            ProcentMin = GetString(limit, "min_green_label"),
                            ProcentText = GetString(limit, "text_green"),

                            ResultAverage = GetString(res, "AvrResult"),
                            ResultMin = GetString(res, "MinResult"),
                            ResultMax = GetString(res, "MaxResult"),
                            ResultColor = FromStringHelper.ToInt(GetString(res, "result_color")),

                            Results = ParseResults(GetString(res, "results")),
                        };

                        test.SubTests.Add(sub);
                    }
                }
            }

            return testResultModel;
        }
    }
}