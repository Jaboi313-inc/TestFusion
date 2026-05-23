using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Core.Models
{
    public class SyncResult
    {
        public TestListItemModel Item { get; set; } = default!;

        public StoredJsonModel StoredJson { get; set; } = default!;
    }
}
