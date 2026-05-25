using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace TestFusion.Core.Models.TestResult
{
    public class TestModel
    {
        public int TestId { get; set; } = default!;
        public string TestName { get; set; } = default!;
        public int TestStatus { get; set; } = default!;
        public int TestTime { get; set; } = default!;
        public string TestType { get; set; } = default!;
        public int TestResponseTime { get; set; } = default!;

        public List<SubTestModel> SubTests { get; set; } = new();

    }
}
