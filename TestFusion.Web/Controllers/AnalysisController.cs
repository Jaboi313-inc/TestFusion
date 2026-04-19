using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestFusion.Web.Controllers
{
    public class AnalysisController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public IActionResult Generate()
        {
            return View();
        }
    }
}
