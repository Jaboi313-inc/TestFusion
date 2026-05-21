using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace TestFusion.Core.Models.TestResult
{
    public class TestModel
    {
        public string TestName { get; set; } = default!;
        public int TestStatus { get; set; } = default!;
        public string TestTime { get; set; } = default!;
        public string TestType { get; set; } = default!;
        public string TestResponseTime { get; set; } = default!;

        public List<SubTestModel> SubTests { get; set; } = new();

    }
}
