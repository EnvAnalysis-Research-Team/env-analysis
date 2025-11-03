using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;

namespace env_analysis_project.Controllers
{
    public class EmissionSourcesController : Controller
    {
        private readonly env_analysis_projectContext _context;

        public EmissionSourcesController(env_analysis_projectContext context)
        {
            _context = context;
        }

        // =============================
        //  LIST VIEW
        // =============================
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách nguồn phát thải và loại nguồn
            var emissionSources = await _context.EmissionSource
                .Include(e => e.SourceType)
                .OrderBy(e => e.SourceName)
                .ToListAsync();

            ViewBag.SourceTypes = await _context.SourceType
                .OrderBy(t => t.SourceTypeName)
                .ToListAsync();

            return View(emissionSources);
        }

        // =============================
        //  DETAIL (AJAX)
        // =============================
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var source = await _context.EmissionSource
                .Include(e => e.SourceType)
                .FirstOrDefaultAsync(e => e.EmissionSourceID == id);

            if (source == null)
                return NotFound();

            var result = new
            {
                source.EmissionSourceID,
                source.SourceCode,
                source.SourceName,
                source.Description,
                source.Location,
                source.Latitude,
                source.Longitude,
                source.IsActive,
                source.CreatedAt,
                source.UpdatedAt,
                source.SourceTypeID,
                SourceTypeName = source.SourceType?.SourceTypeName
            };

            return Json(result);
        }

        // =============================
        //  CREATE (FORM)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] EmissionSource model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreatedAt = DateTime.Now;

            // Nếu người dùng để trống => gán null
            model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;

            _context.EmissionSource.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Emission source created successfully!" });
        }

        // =============================
        //  EDIT (AJAX)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [FromForm] EmissionSource model)
        {
            if (id != model.EmissionSourceID)
                return BadRequest(new { error = "Invalid ID" });

            var existing = await _context.EmissionSource.FindAsync(id);
            if (existing == null)
                return NotFound(new { error = "Emission Source not found" });

            if (!ModelState.IsValid)
                return BadRequest(new { error = "Invalid data" });

            // Cập nhật thủ công từng trường
            existing.SourceCode = model.SourceCode;
            existing.SourceName = model.SourceName;
            existing.SourceTypeID = model.SourceTypeID;
            existing.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;
            existing.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Emission source updated successfully!" });
        }

        // =============================
        //  DELETE (AJAX)
        // =============================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var emissionSource = await _context.EmissionSource.FindAsync(id);
            if (emissionSource == null)
                return NotFound(new { error = "Emission Source not found" });

            _context.EmissionSource.Remove(emissionSource);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Emission source deleted successfully!" });
        }

        // =============================
        //  HELPER
        // =============================
        private bool EmissionSourceExists(int id)
        {
            return _context.EmissionSource.Any(e => e.EmissionSourceID == id);
        }
    }
}
