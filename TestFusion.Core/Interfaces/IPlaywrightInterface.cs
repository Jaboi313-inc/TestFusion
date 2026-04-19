using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.Core.Models;

namespace TestFusion.Core.Interfaces
{
    public interface IPlaywrightInterface
    {
        Task<List<string>> GetAllIDs();
        Task<TestListItemModel> GetDataForId(string id);
    }
}
