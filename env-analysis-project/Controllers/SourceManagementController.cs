using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;

namespace env_analysis_project.Controllers
{
    public class SourceManagementController : Controller
    {
        private readonly env_analysis_projectContext _context;

        public SourceManagementController(env_analysis_projectContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Serve Manage view with emission sources model and source types (used by the sidebar)
        public async Task<IActionResult> Manage()
        {
            var sources = await _context.EmissionSource
                .Include(e => e.SourceType)
                .OrderBy(e => e.SourceName)
                .ToListAsync();

            var sourceTypes = await _context.SourceType
                .Select(st => new
                {
                    st.SourceTypeID,
                    st.SourceTypeName,
                    st.Description,
                    st.IsActive,
                    st.CreatedAt,
                    st.UpdatedAt,
                    Count = _context.EmissionSource.Count(es => es.SourceTypeID == st.SourceTypeID)
                })
                .ToListAsync();

            ViewBag.SourceTypes = sourceTypes;
            return View("Manage", sources);
        }
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var sources = await _context.EmissionSource
                .Include(e => e.SourceType)
                .OrderBy(e => e.SourceName)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Source Name,Source Code,Location,Latitude,Longitude,Source Type,Status");
            foreach (var source in sources)
            {
                var fields = new[]
                {
                    EscapeCsv(source.SourceName),
                    EscapeCsv(source.SourceCode),
                    EscapeCsv(source.Location),
                    EscapeCsv(source.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    EscapeCsv(source.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    EscapeCsv(source.SourceType?.SourceTypeName),
                    EscapeCsv(source.IsActive ? "Active" : "Inactive")
                };
                builder.AppendLine(string.Join(",", fields));
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var fileName = $"emission-sources-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var sanitized = value.Replace("\"", "\"\"");
            return $"\"{sanitized}\"";
        }

    }
}
