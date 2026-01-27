using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using env_analysis_project.Data;
using env_analysis_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace env_analysis_project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly env_analysis_projectContext _context;

        public HomeController(ILogger<HomeController> logger, env_analysis_projectContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var emissionSources = await _context.EmissionSource
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SourceName)
                .Select(s => new
                {
                    Id = s.EmissionSourceID,
                    Label = s.SourceName
                })
                .ToListAsync();

            var parameters = await _context.Parameter
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ParameterName)
                .Select(p => new
                {
                    Code = p.ParameterCode,
                    Label = p.ParameterName,
                    Unit = p.Unit,
                    Type = ParameterTypeHelper.Normalize(p.Type)
                })
                .ToListAsync();

            ViewBag.EmissionSources = emissionSources;
            ViewBag.Parameters = parameters;

            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
