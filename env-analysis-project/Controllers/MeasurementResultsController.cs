using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;

namespace env_analysis_project.Controllers
{
    public class MeasurementResultsController : Controller
    {
        private readonly env_analysis_projectContext _context;

        public MeasurementResultsController(env_analysis_projectContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Manage()
        {
            var emissionSources = await _context.EmissionSource
                .OrderBy(s => s.SourceName)
                .Select(s => new LookupOption
                {
                    Id = s.EmissionSourceID,
                    Label = s.SourceName
                })
                .ToListAsync();

            var parameters = await _context.Parameter
                .OrderBy(p => p.ParameterName)
                .Select(p => new ParameterLookup
                {
                    Code = p.ParameterCode,
                    Label = p.ParameterName
                })
                .ToListAsync();

            ViewBag.EmissionSources = emissionSources;
            ViewBag.Parameters = parameters;

            return View("Manage");
        }

        // GET: MeasurementResults
        public async Task<IActionResult> Index()
        {
            var env_analysis_projectContext = _context.MeasurementResult.Include(m => m.EmissionSource).Include(m => m.Parameter);
            return View(await env_analysis_projectContext.ToListAsync());
        }

        // GET: MeasurementResults/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measurementResult = await _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .FirstOrDefaultAsync(m => m.ResultID == id);
            if (measurementResult == null)
            {
                return NotFound();
            }

            return View(measurementResult);
        }

        // GET: MeasurementResults/Create
        public IActionResult Create()
        {
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource, "EmissionSourceID", "SourceCode");
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>(), "ParameterCode", "ParameterCode");
            return View();
        }

        // POST: MeasurementResults/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ResultID,EmissionSourceID,ParameterCode,MeasurementDate,Value,Unit,EntryDate,Remark,IsApproved,ApprovedAt")] MeasurementResult measurementResult)
        {
            if (ModelState.IsValid)
            {
                _context.Add(measurementResult);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource, "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>(), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
            return View(measurementResult);
        }

        // GET: MeasurementResults/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measurementResult = await _context.MeasurementResult.FindAsync(id);
            if (measurementResult == null)
            {
                return NotFound();
            }
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource, "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>(), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
            return View(measurementResult);
        }

        // POST: MeasurementResults/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ResultID,EmissionSourceID,ParameterCode,MeasurementDate,Value,Unit,EntryDate,Remark,IsApproved,ApprovedAt")] MeasurementResult measurementResult)
        {
            if (id != measurementResult.ResultID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(measurementResult);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeasurementResultExists(measurementResult.ResultID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource, "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>(), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
            return View(measurementResult);
        }

        // GET: MeasurementResults/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measurementResult = await _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .FirstOrDefaultAsync(m => m.ResultID == id);
            if (measurementResult == null)
            {
                return NotFound();
            }

            return View(measurementResult);
        }

        // POST: MeasurementResults/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var measurementResult = await _context.MeasurementResult.FindAsync(id);
            if (measurementResult != null)
            {
                _context.MeasurementResult.Remove(measurementResult);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ListData(string type)
        {
            var normalizedType = NormalizeType(type);

            var query = _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .AsQueryable();

            var results = await query
                .OrderByDescending(m => m.MeasurementDate)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> DetailData(int id)
        {
            var measurement = await _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .FirstOrDefaultAsync(m => m.ResultID == id);

            if (measurement == null)
            {
                return NotFound(new { success = false, error = "Measurement result not found." });
            }

            return Json(ToDto(measurement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] MeasurementResultRequest request)
        {
            if (!ModelState.IsValid || request == null)
            {
                return BadRequest(new { success = false, error = "Invalid measurement result payload." });
            }

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId))
            {
                return BadRequest(new { success = false, error = "Emission source not found." });
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode))
            {
                return BadRequest(new { success = false, error = "Parameter not found." });
            }

            var entity = new MeasurementResult
            {
                EmissionSourceID = request.EmissionSourceId,
                ParameterCode = request.ParameterCode,
                MeasurementDate = request.MeasurementDate,
                Value = request.Value,
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(),
                EntryDate = DateTime.UtcNow,
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                IsApproved = request.IsApproved,
                ApprovedAt = request.IsApproved ? request.ApprovedAt : null,
                type = NormalizeType(request.Type)
            };

            _context.MeasurementResult.Add(entity);
            await _context.SaveChangesAsync();

            var dto = await _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .Where(m => m.ResultID == entity.ResultID)
                .Select(m => ToDto(m))
                .FirstAsync();

            return Json(new { success = true, message = "Measurement result created successfully.", data = dto });
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAjax(int id, [FromBody] MeasurementResultRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, error = "Invalid measurement result payload." });
            }

            var entity = await _context.MeasurementResult.FindAsync(id);
            if (entity == null)
            {
                return NotFound(new { success = false, error = "Measurement result not found." });
            }

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId))
            {
                return BadRequest(new { success = false, error = "Emission source not found." });
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode))
            {
                return BadRequest(new { success = false, error = "Parameter not found." });
            }

            entity.EmissionSourceID = request.EmissionSourceId;
            entity.ParameterCode = request.ParameterCode;
            entity.MeasurementDate = request.MeasurementDate;
            entity.Value = request.Value;
            entity.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
            entity.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
            entity.IsApproved = request.IsApproved;
            entity.ApprovedAt = request.IsApproved ? request.ApprovedAt : null;
            entity.type = NormalizeType(request.Type ?? entity.type);

            await _context.SaveChangesAsync();

            var dto = await _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .Where(m => m.ResultID == entity.ResultID)
                .Select(m => ToDto(m))
                .FirstAsync();

            return Json(new { success = true, message = "Measurement result updated successfully.", data = dto });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var entity = await _context.MeasurementResult.FindAsync(id);
            if (entity == null)
            {
                return NotFound(new { success = false, error = "Measurement result not found." });
            }

            _context.MeasurementResult.Remove(entity);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Measurement result deleted successfully." });
        }

        private bool MeasurementResultExists(int id)
        {
            return _context.MeasurementResult.Any(e => e.ResultID == id);
        }

        private static string NormalizeType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "water";

            var normalized = input.Trim().ToLowerInvariant();
            return normalized is "water" or "air" ? normalized : "water";
        }

        private static MeasurementResultDto ToDto(MeasurementResult measurement)
        {
            return new MeasurementResultDto
            {
                ResultID = measurement.ResultID,
                Type = NormalizeType(measurement.type),
                EmissionSourceID = measurement.EmissionSourceID,
                EmissionSourceName = measurement.EmissionSource?.SourceName ?? $"Source #{measurement.EmissionSourceID}",
                ParameterCode = measurement.ParameterCode,
                ParameterName = measurement.Parameter?.ParameterName ?? measurement.ParameterCode,
                MeasurementDate = measurement.MeasurementDate,
                Value = measurement.Value,
                Unit = measurement.Unit,
                Remark = measurement.Remark,
                IsApproved = measurement.IsApproved,
                ApprovedAt = measurement.ApprovedAt
            };
        }

        private sealed class LookupOption
        {
            public int Id { get; set; }
            public string Label { get; set; }
        }

        private sealed class ParameterLookup
        {
            public string Code { get; set; }
            public string Label { get; set; }
        }

        public sealed class MeasurementResultDto
        {
            public int ResultID { get; set; }
            public string Type { get; set; }
            public int EmissionSourceID { get; set; }
            public string EmissionSourceName { get; set; }
            public string ParameterCode { get; set; }
            public string ParameterName { get; set; }
            public DateTime? MeasurementDate { get; set; }
            public double? Value { get; set; }
            public string Unit { get; set; }
            public string Remark { get; set; }
            public bool IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }

        public sealed class MeasurementResultRequest
        {
            public string Type { get; set; }
            public int EmissionSourceId { get; set; }
            public string ParameterCode { get; set; }
            public DateTime MeasurementDate { get; set; }
            public double? Value { get; set; }
            public string Unit { get; set; }
            public bool IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string Remark { get; set; }
        }
    }
}
