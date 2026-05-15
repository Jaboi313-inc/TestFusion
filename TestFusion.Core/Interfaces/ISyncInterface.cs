using System;
using System.Collections.Generic;
using System.Text;

namespace TestFusion.Core.Interfaces
{
    public interface ISyncService
    {
        Task RunSync(CancellationToken ct = default);
    }
}
