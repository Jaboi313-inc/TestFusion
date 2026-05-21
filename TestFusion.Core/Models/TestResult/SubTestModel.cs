using System.ComponentModel.DataAnnotations;

namespace TestFusion.Core.Models.TestResult
{
    public class SubTestModel
    {
        public string TankName { get; set; } = default!;
        public int TankPosition { get; set; } = default!;

        public decimal Max { get; set; } = default!;
        public decimal Min { get; set; } = default!;

        public decimal BlueMax { get; set; } = default!;
        public decimal BlueMin { get; set; } = default!;
        public decimal YellowMax { get; set; } = default!;
        public decimal YellowMin { get; set; } = default!;

        public decimal TolBlue { get; set; } = default!;

        public string ProcentMax { get; set; } = default!;
        public string ProcentMin { get; set; } = default!;
        public string ProcentText { get; set; } = default!;

        public string ResultMax { get; set; } = default!;
        public string ResultMin { get; set; } = default!;
        public string ResultAverage { get; set; } = default!;
        public int ResultColor { get; set; } = default!;

        public List<decimal> Results { get; set; } = new();

    }
}
