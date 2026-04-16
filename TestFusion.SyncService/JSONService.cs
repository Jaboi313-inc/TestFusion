using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TestFusion.SyncService
{
    public class JSONService
    {
        public List<string> ConvertToSimpleJSON(string json)
        {
            return GetTestNames(json);
        }

        private List<string> GetTestNames(string json)
        {
            var result = new List<string>();

            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var pages = root.GetProperty("TestsDataPages");

            foreach (var page in pages.EnumerateArray())
            {
                var tests = page.GetProperty("TestData");

                foreach (var test in tests.EnumerateArray())
                {
                    var testName = test
                        .GetProperty("TestData")
                        .GetProperty("test_name")
                        .GetString();

                    result.Add(testName);
                }
            }

            return result;
        }
    }
}
