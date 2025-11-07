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
            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(entry => entry.Value?.Errors?.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? $"Invalid value for {entry.Key}"
                            : error.ErrorMessage))
                    .ToList();

                if (isAjax)
                {
                    return BadRequest(new { success = false, errors });
                }

                return BadRequest(ModelState);
            }

            model.CreatedAt = DateTime.Now;

            // Nếu người dùng để trống => gán null
            model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description;

            _context.EmissionSource.Add(model);
            await _context.SaveChangesAsync();

            var response = new
            {
                success = true,
                message = "Emission source created successfully!",
                data = new
                {
                    model.EmissionSourceID,
                    model.SourceCode,
                    model.SourceName,
                    model.SourceTypeID,
                    model.Location,
                    model.Latitude,
                    model.Longitude,
                    model.IsActive
                }
            };

            if (isAjax)
            {
                return Json(response);
            }

            TempData["SuccessMessage"] = response.message;
            return RedirectToAction(nameof(Index));
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] DeleteEmissionSourceRequest request)
        {
            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (request == null || request.Id <= 0)
            {
                const string invalidMessage = "Invalid emission source identifier.";
                if (isAjax)
                {
                    return BadRequest(new { success = false, error = invalidMessage });
                }

                TempData["ErrorMessage"] = invalidMessage;
                return RedirectToAction(nameof(Index));
            }

            var emissionSource = await _context.EmissionSource.FindAsync(request.Id);
            if (emissionSource == null)
            {
                if (isAjax)
                {
                    return NotFound(new { success = false, error = "Emission Source not found." });
                }

                TempData["ErrorMessage"] = "Emission Source not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.EmissionSource.Remove(emissionSource);
            await _context.SaveChangesAsync();

            var response = new { success = true, message = "Emission source deleted successfully!" };
            if (isAjax)
            {
                return Json(response);
            }

            TempData["SuccessMessage"] = response.message;
            return RedirectToAction(nameof(Index));
        }


        // =============================
        //  HELPER
        // =============================
        private bool EmissionSourceExists(int id)
        {
            return _context.EmissionSource.Any(e => e.EmissionSourceID == id);
        }

        public sealed class DeleteEmissionSourceRequest
        {
            public int Id { get; set; }
        }
    }
}
