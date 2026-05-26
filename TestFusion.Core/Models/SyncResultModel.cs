using System;
using System.Collections.Generic;
using System.Text;

namespace TestFusion.Core.Models
{
    public class SyncResult
    {
        public TestListItemModel Item { get; set; } = default!;

        public StoredJsonModel StoredJson { get; set; } = default!;
    }
}
