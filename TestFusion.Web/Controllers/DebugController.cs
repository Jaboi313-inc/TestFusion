using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestFusion.Core.Interfaces;
using TestFusion.Data;

namespace TestFusion.Web.Controllers
{
    public class DebugController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
    }
}