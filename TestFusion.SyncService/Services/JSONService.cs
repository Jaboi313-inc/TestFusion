using System.Globalization;

using System.Text.Json;
using TestFusion.Core.Models.TestResult;
using TestFusion.Core.Models.WebModels;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService.Services
{
    public class JSONService
    {
        public SimpleJSON ConvertToSimpleJSON(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var jsonFile = new SimpleJSON
            {
                _id = root.TryGetProperty("_id", out var id) ? id.GetString() : null,
                actuator_code = root.TryGetProperty("actuator_code", out var ac) ? ac.GetString() : null,
                actuator_Brand = root.TryGetProperty("actuator_Brand", out var ab) ? ab.GetString() : null,
                actuator_type = root.TryGetProperty("actuator_type", out var at) ? at.GetString() : null,
                notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null,
                datetime = root.TryGetProperty("datetime", out var dt) ? dt.GetString() : null,
                tests = new List<Test>()
            };

            var pages = root.GetProperty("TestsDataPages");

            foreach (var page in pages.EnumerateArray())
            {
                if (!page.TryGetProperty("TestData", out var testDataArray))
                    continue;

                foreach (var item in testDataArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("TestData", out var testData))
                        continue;

                    var test = new Test
                    {
                        test_id = testData.TryGetProperty("test_id", out var testId) ? testId.GetInt32() : 0,
                        test_name = testData.TryGetProperty("test_name", out var testName) ? testName.GetString() : null,
                        test_time = testData.TryGetProperty("test_time", out var testTime) ? testTime.GetString() : null,
                        results = ""
                    };

                    var results = new List<string>();

                    if (item.TryGetProperty("TestResults", out var tr) &&
                        tr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in tr.EnumerateArray())
                        {
                            if (r.TryGetProperty("results", out var value))
                            {
                                var str = value.GetString();

                                if (!string.IsNullOrWhiteSpace(str))
                                    results.Add(str);
                            }
                        }
                    }

                    test.results = string.Join(" | ", results);

                    jsonFile.tests.Add(test);
                }
            }

            return jsonFile;
        }

        public TestListItemModel ConvertToTestListItem(string json)
        {
            var jsonFile = ConvertToTestResultModel(json);

            return new TestListItemModel
            {
                Id = jsonFile.Id,
                PartNumber = jsonFile.PartNumber,
                PartBrand = jsonFile.PartBrand,
                PartType = jsonFile.PartType,
                DateTime = jsonFile.TimeOffTesting
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
            if (!rspResults.TryGetProperty("dataArray", out var arr))
                return "--";

            foreach (var item in arr.EnumerateArray())
            {
                if (item.TryGetProperty("result", out var r))
                    return r.GetString() ?? "--";
            }

            return "--";
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

            var jsonFile = new TestResultModel
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
                            TestName = GetString(testData, "test_name"),
                            TestStatus = GetInt(testData, "status"),
                            TestTime = GetString(item, "test_time"),

                            TestType = "",
                            TestResponseTime = "",

                            SubTests = new List<SubTestModel>()
                        };

                        jsonFile.Tests.Add(alteredTest);

                        continue;
                    }

                    item.TryGetProperty("RspResults", out var rspResults);

                    var test = new TestModel
                    {
                        TestName = GetString(testData, "test_name"),
                        TestStatus = GetInt(testData, "status"),
                        TestTime = GetString(item, "test_time"),
                        TestType = GetString(rspResults, "type"),
                        TestResponseTime = GetFirstResult(rspResults),

                        SubTests = new List<SubTestModel>()
                    };                    

                    jsonFile.Tests.Add(test);

                    item.TryGetProperty("TestLimits", out var limits);
                    item.TryGetProperty("TestResults", out var results);                    

                    var resultLookup = new Dictionary<string, JsonElement>();

                    if (results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in results.EnumerateArray())
                        {
                            var pos = GetString(r, "tank_position");
                            if (!string.IsNullOrEmpty(pos))
                                resultLookup[pos] = r;
                        }
                    }

                    foreach (var limit in limits.EnumerateArray())
                    {
                        var position = GetInt(limit, "tank_position");

                        resultLookup.TryGetValue(position.ToString(), out var res);

                        var sub = new SubTestModel
                        {
                            TankName = GetString(limit, "tank_name"),
                            TankPosition = GetInt(limit, "tank_position"),

                            Min = GetString(limit, "min_green"),
                            Max = GetString(limit, "max_green"),

                            BlueMin = GetString(limit, "min_blue"),
                            BlueMax = GetString(limit, "max_blue"),
                            YellowMin = GetString(limit, "min_yellow"),
                            YellowMax = GetString(limit, "max_yellow"),

                            TolBlue = GetString(limit, "tol_blue"),

                            ProcentMax = GetString(limit, "max_green_label"),
                            ProcentMin = GetString(limit, "min_green_label"),
                            ProcentText = GetString(limit, "text_green"),

                            ResultAverage = GetString(res, "AvrResult"),
                            ResultMin = GetString(res, "MinResult"),
                            ResultMax = GetString(res, "MaxResult"),
                            ResultColor = GetString(res, "result_color"),

                            Results = ParseResults(GetString(res, "results")),
                        };

                        test.SubTests.Add(sub);
                    }
                }
            }

            return jsonFile;
        }
    }
}