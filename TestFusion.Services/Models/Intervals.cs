using System;
using System.Collections.Generic;
using System.Text;

namespace TestFusion.Services.Models
{
    public class Intervals
    {
        public TimeSpan DatafetchingInterval { get; set; }
        public TimeSpan WorkerHeartbeatInterval { get; set; }
    }
}
