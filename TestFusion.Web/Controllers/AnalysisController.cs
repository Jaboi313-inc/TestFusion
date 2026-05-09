using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestFusion.Core.Interfaces;
using TestFusion.Core.Models;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly IPlaywrightInterface _playwrightInterface;

        public AnalysisController(IPlaywrightInterface playwrightInterface)
        {
            _playwrightInterface = playwrightInterface;
        }

        public async Task<IActionResult> Index()
        {
            var ids = await _playwrightInterface.GetAllIDs();

            var item = await _playwrightInterface.GetDataForId(ids.FirstOrDefault());

            var semaphore = new SemaphoreSlim(1); // Limit to # of concurrent tasks

            var tasks = ids.Take(5).Select(async id =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _playwrightInterface.GetDataForId(id);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var items = await Task.WhenAll(tasks);

            return View(items.ToList());

            /*
            var debugJson = JsonSerializer.Serialize(
            item,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

            System.IO.File.WriteAllText(
                @"D:\Progammeren\repos\TestFusion\TestFusion.Web\mapped.json",
                debugJson
            );

            return View(new List<TestListItemModel> { item });
            */

            /*
            var items = await Task.WhenAll(
                ids.Select(id => _playwrightInterface.GetDataForId(id))
            );

            return View(items.ToList());
            */
        }

        [HttpPost]
        public IActionResult Generate(List<string> selectedIds)
        {
            var model = new GeneratedModel
            {
                SelectedIds = selectedIds
            };

            return View(model);
        }
    }
}
