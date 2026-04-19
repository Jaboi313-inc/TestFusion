using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            var items = ids
                .Select(id => _playwrightInterface.GetDataForId(id))
                .ToList();

            return View(items);
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
