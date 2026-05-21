namespace TestFusion.Core.Models.TestResult
{
    public class TestResultModel
    {
        public string Id { get; set; } = default!;

        public DateTimeOffset TimeOffTesting { get; set; }

        public string PartNumber { get; set; } = default!;
        public string PartBrand { get; set; } = default!;
        public string PartType { get; set; } = default!;

        public string CustomerName { get; set; } = default!;
        public string CustomerPhone { get; set; } = default!;
        public string CustomerMail { get; set; } = default!;
        public string CustomerNotes { get; set; } = default!;

        public string TestNotes { get; set; } = default!;

        public List<TestModel> Tests { get; set; } = new();
    }
}
