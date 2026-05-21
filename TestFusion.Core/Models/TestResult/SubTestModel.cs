using System.ComponentModel.DataAnnotations;

namespace TestFusion.Core.Models.TestResult
{
    public class SubTestModel
    {
        public string TankName { get; set; } = default!;
        public int TankPosition { get; set; } = default!;

        public string Max { get; set; } = default!;
        public string Min { get; set; } = default!;

        public string BlueMax { get; set; } = default!;
        public string BlueMin { get; set; } = default!;
        public string YellowMax { get; set; } = default!;
        public string YellowMin { get; set; } = default!;

        public string TolBlue { get; set; } = default!;

        public string ProcentMax { get; set; } = default!;
        public string ProcentMin { get; set; } = default!;
        public string ProcentText { get; set; } = default!;

        public string ResultMax { get; set; } = default!;
        public string ResultMin { get; set; } = default!;
        public string ResultAverage { get; set; } = default!;
        public string ResultColor { get; set; } = default!;

        public List<decimal> Results { get; set; } = new();

    }
}
