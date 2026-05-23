using System;
using System.Collections.Generic;
using System.Text;
using TestFusion.Core.Models.WebModels;

namespace TestFusion.Core.Interfaces
{
    public interface IPlaywright
    {
        Task<List<string>> GetAllIDs();
        Task<string> GetDataForId(string id);
    }
}
