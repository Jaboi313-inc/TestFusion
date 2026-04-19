using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TestFusion.SyncService.Models;

namespace TestFusion.SyncService
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
    }
}
