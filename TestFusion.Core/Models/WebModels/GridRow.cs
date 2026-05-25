using System;
using System.Collections.Generic;
using System.Text;

namespace TestFusion.Core.Models.WebModels
{
    public class GridRow
    {
        public string TestName { get; set; } = "";
        public List<TestCellModel> Cells { get; set; } = new();
    }
}
