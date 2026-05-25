using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.Core.Models.TestResult;

namespace TestFusion.Core.Models.WebModels
{
    public class TestResultViewModel
    {
        public TestResultModel Data { get; set; } = default!;
        public List<TestCellModel> NormalizedTests { get; set; } = new();
    }
}
