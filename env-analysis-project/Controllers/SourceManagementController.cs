using Microsoft.AspNetCore.Mvc;

namespace env_analysis_project.Controllers
{
    public class SourceManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Manage()
        {
            return View("Manage");
        }
    }
}
