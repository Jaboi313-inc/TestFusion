using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestFusion.Web.Models;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        public ActionResult Index()
        {
            var items = new List<TestListItemModel>
            {
                new TestListItemModel { Id = "1", PartNumber = "001", PartBrand = "Bosch", PartType = "X1", DateTime = DateTime.Now },
                new TestListItemModel { Id = "2", PartNumber = "002", PartBrand = "Delphi", PartType = "Y2", DateTime = DateTime.Now },
                new TestListItemModel { Id = "3", PartNumber = "003", PartBrand = "Denso", PartType = "Z3", DateTime = DateTime.Now }
            };

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
