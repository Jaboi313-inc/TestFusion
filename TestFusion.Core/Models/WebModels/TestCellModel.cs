using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.Core.Models.TestResult;

namespace TestFusion.Core.Models.WebModels
{
    public class TestCellModel
    {
        public int TestId { get; set; } = default!;
        public string TestName { get; set; } = default!;
        public bool Exists { get; set; }
        public bool IsSkipped { get; set; }
        public int? Status { get; set; }
        public double? Time { get; set; }
        public string? Response { get; set; }
        public List<SubTestModel> SubTests { get; set; } = new();
    }
}
