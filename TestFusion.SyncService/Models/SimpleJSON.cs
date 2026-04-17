using System;
using System.Collections.Generic;
using System.Text;

namespace TestFusion.SyncService.Models
{
    public class SimpleJSON
    {
        public string _id { get; set; }
        public string actuator_code { get; set; }
        public string actuator_Brand { get; set; }
        public string actuator_type { get; set; }
        public string notes { get; set; }
        public string datetime { get; set; }

        public List<Test> tests { get; set; } = new();
    }

    public class Test
    {
        public int test_id { get; set; }
        public string test_name { get; set; }
        public string test_time { get; set; }
        public string results { get; set; }
    }
}
