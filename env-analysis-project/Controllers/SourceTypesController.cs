using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;

namespace env_analysis_project.Controllers
{
    public class SourceTypesController : Controller
    {
        private readonly env_analysis_projectContext _context;

        public SourceTypesController(env_analysis_projectContext context)
        {
            _context = context;
        }

        // GET: SourceTypes
        // Keeps existing Index view behavior (for full page listing)
        public async Task<IActionResult> Index()
        {
            var sourceTypes = await _context.SourceType
                .Select(st => new
                {
                    st.SourceTypeID,
                    st.SourceTypeName,
                    st.Description,
                    st.IsActive,
                    st.CreatedAt,
                    st.UpdatedAt,
                    Count = _context.EmissionSource
                        .Count(es => es.SourceTypeID == st.SourceTypeID)
                })
                .ToListAsync();

            ViewBag.SourceTypes = sourceTypes;
            return View(sourceTypes);
        }

        // New endpoint: return JSON list of source types (used by AJAX or embedded widgets)
        // GET: /SourceTypes/GetList
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var list = await _context.SourceType
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

            return Json(list);
        }

        // New endpoint: return JSON details for a single source type (for modal population)
        // GET: /SourceTypes/Get/5
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var st = await _context.SourceType
                .Where(s => s.SourceTypeID == id)
                .Select(s => new
                {
                    s.SourceTypeID,
                    s.SourceTypeName,
                    s.Description,
                    s.IsActive,
                    s.CreatedAt,
                    s.UpdatedAt,
                    Count = _context.EmissionSource.Count(es => es.SourceTypeID == s.SourceTypeID)
                })
                .FirstOrDefaultAsync();

            if (st == null) return NotFound();
            return Json(st);
        }

        // GET: SourceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sourceType = await _context.SourceType
                .FirstOrDefaultAsync(m => m.SourceTypeID == id);
            if (sourceType == null) return NotFound();

            return View(sourceType);
        }

        // GET: SourceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SourceTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SourceType sourceType)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return Json(new { success = false, errors });
                }

                TempData["Error"] = "Invalid data. Please check again.";
                return RedirectToAction("Manage", "SourceManagement");
            }

            sourceType.CreatedAt = DateTime.Now;
            sourceType.UpdatedAt = DateTime.Now;

            _context.Add(sourceType);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    id = sourceType.SourceTypeID,
                    name = sourceType.SourceTypeName
                });
            }

            TempData["Success"] = "Source type created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SourceTypeID,SourceTypeName,Description,IsActive,CreatedAt,UpdatedAt")] SourceType model)
        {
            // Kiểm tra ID hợp lệ
            if (id != model.SourceTypeID)
                return Json(new { success = false, message = "Invalid ID." });

            // Kiểm tra model hợp lệ
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();
                return Json(new { success = false, errors });
            }

            // Tìm bản ghi hiện có
            var existing = await _context.SourceType.FindAsync(id);
            if (existing == null)
                return Json(new { success = false, message = "Source type not found." });

            try
            {
                // Cập nhật các thuộc tính
                existing.SourceTypeName = model.SourceTypeName?.Trim();
                existing.Description = model.Description?.Trim();
                existing.IsActive = model.IsActive;
                existing.UpdatedAt = DateTime.Now; // luôn cập nhật tại server

                // Lưu thay đổi
                _context.Update(existing);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    id = existing.SourceTypeID,
                    name = existing.SourceTypeName
                });
            }
            catch (Exception ex)
            {
                // Log nếu cần, trả về lỗi JSON gọn gàng
                return Json(new { success = false, message = "Error updating source type: " + ex.Message });
            }
        }

        // GET: SourceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var sourceType = await _context.SourceType.FirstOrDefaultAsync(m => m.SourceTypeID == id);
            if (sourceType == null) return NotFound();

            return View(sourceType);
        }

        // POST: SourceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sourceType = await _context.SourceType.FindAsync(id);
            if (sourceType != null) _context.SourceType.Remove(sourceType);

            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SourceTypeExists(int id)
        {
            return _context.SourceType.Any(e => e.SourceTypeID == id);
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}