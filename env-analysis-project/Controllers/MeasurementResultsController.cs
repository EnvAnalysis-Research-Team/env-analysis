using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;
using env_analysis_project.Validators;

namespace env_analysis_project.Controllers
{
    public class MeasurementResultsController : Controller
    {
        private readonly env_analysis_projectContext _context;
        private const int TrendParameterLimit = 4;

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
                    Label = p.ParameterName,
                    Unit = p.Unit
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
        public async Task<IActionResult> ListData(string? type)
        {
            var normalizedType = string.IsNullOrWhiteSpace(type) ? null : NormalizeType(type);

            var query = _context.MeasurementResult
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .AsQueryable();

            if (!string.IsNullOrEmpty(normalizedType))
            {
                query = query.Where(m => m.type == normalizedType);
            }

            var results = await query
                .OrderByDescending(m => m.MeasurementDate)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Ok(ApiResponse.Success(results));
        }

        [HttpGet]
        public async Task<IActionResult> ParameterTrends([FromQuery] string[] codes)
        {
            var normalizedCodes = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in codes ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var trimmed = code.Trim();
                if (seen.Add(trimmed))
                {
                    normalizedCodes.Add(trimmed);
                }
            }

            if (normalizedCodes.Count == 0)
            {
                return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("At least one parameter must be selected."));
            }

            if (normalizedCodes.Count > TrendParameterLimit)
            {
                return BadRequest(ApiResponse.Fail<ParameterTrendResponse>($"You can compare at most {TrendParameterLimit} parameters."));
            }

            var normalizedCodesUpper = normalizedCodes
                .Select(c => c.ToUpperInvariant())
                .ToList();

            var aggregates = await _context.MeasurementResult
                .Where(m => normalizedCodesUpper.Contains(m.ParameterCode.ToUpper()) &&
                            m.Value.HasValue)
                .Select(m => new
                {
                    m.ParameterCode,
                    Date = m.MeasurementDate == default ? m.EntryDate : m.MeasurementDate,
                    m.Value
                })
                .GroupBy(x => new { x.ParameterCode, Year = x.Date.Year, Month = x.Date.Month })
                .Select(g => new ParameterAggregate
                {
                    ParameterCode = g.Key.ParameterCode,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Min = g.Min(x => x.Value),
                    Max = g.Max(x => x.Value)
                })
                .ToListAsync();

            if (!aggregates.Any())
            {
                return Ok(ApiResponse.Success(new ParameterTrendResponse()));
            }

            var availableMonths = aggregates
                .Select(a => new { a.Year, a.Month })
                .Distinct()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(12)
                .ToList();

            availableMonths.Reverse();

            var monthKeySet = availableMonths
                .Select(m => (m.Year, m.Month))
                .ToHashSet();

            var filteredAggregates = aggregates
                .Where(a => monthKeySet.Contains((a.Year, a.Month)))
                .ToList();

            var parameterMetadata = await _context.Parameter
                .Where(p => normalizedCodesUpper.Contains(p.ParameterCode.ToUpper()))
                .Select(p => new ParameterLookup
                {
                    Code = p.ParameterCode,
                    Label = p.ParameterName,
                    Unit = p.Unit
                })
                .ToListAsync();

            var metadataLookup = parameterMetadata
                .GroupBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var aggregateLookup = filteredAggregates.ToDictionary(
                agg => CreateAggregateKey(agg.ParameterCode, agg.Year, agg.Month),
                agg => agg,
                StringComparer.OrdinalIgnoreCase);

            var months = availableMonths.Select(m => new DateTime(m.Year, m.Month, 1)).ToList();
            var labels = months.Select(dt => dt.ToString("MMM yyyy")).ToArray();

            var series = normalizedCodes.Select(code =>
            {
                metadataLookup.TryGetValue(code, out var meta);
                var points = months.Select(month =>
                {
                    aggregateLookup.TryGetValue(CreateAggregateKey(code, month.Year, month.Month), out var aggregate);
                    return new ParameterTrendPoint
                    {
                        Month = $"{month:yyyy-MM}",
                        Label = month.ToString("MMM yyyy"),
                        Min = aggregate?.Min,
                        Max = aggregate?.Max
                    };
                }).ToArray();

                return new ParameterTrendSeries
                {
                    ParameterCode = meta?.Code ?? code,
                    ParameterName = meta?.Label ?? meta?.Code ?? code,
                    Unit = meta?.Unit,
                    Points = points
                };
            }).ToArray();

            var response = new ParameterTrendResponse
            {
                Labels = labels,
                Series = series
            };

            return Ok(ApiResponse.Success(response));
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
                return NotFound(ApiResponse.Fail<MeasurementResultDto>("Measurement result not found."));
            }

            return Ok(ApiResponse.Success(ToDto(measurement)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] MeasurementResultRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Invalid measurement result payload."));
            }

            var validationErrors = MeasurementResultValidator.Validate(request).ToList();
            if (!ModelState.IsValid)
            {
                validationErrors.AddRange(GetModelErrors());
            }

            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Invalid measurement result payload.", validationErrors));
            }

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Emission source not found."));
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Parameter not found."));
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

            return Ok(ApiResponse.Success(dto, "Measurement result created successfully."));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAjax(int id, [FromBody] MeasurementResultRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Invalid measurement result payload."));
            }

            var entity = await _context.MeasurementResult.FindAsync(id);
            if (entity == null)
            {
                return NotFound(ApiResponse.Fail<MeasurementResultDto>("Measurement result not found."));
            }

            var validationErrors = MeasurementResultValidator.Validate(request).ToList();
            if (!ModelState.IsValid)
            {
                validationErrors.AddRange(GetModelErrors());
            }

            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Invalid measurement result payload.", validationErrors));
            }

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Emission source not found."));
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Parameter not found."));
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

            return Ok(ApiResponse.Success(dto, "Measurement result updated successfully."));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var entity = await _context.MeasurementResult.FindAsync(id);
            if (entity == null)
            {
                return NotFound(ApiResponse.Fail<object?>("Measurement result not found."));
            }

            _context.MeasurementResult.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object?>(null, "Measurement result deleted successfully."));
        }

        private bool MeasurementResultExists(int id)
        {
            return _context.MeasurementResult.Any(e => e.ResultID == id);
        }

        private static string NormalizeType(string? input)
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
            public string Label { get; set; } = string.Empty;
        }

        private sealed class ParameterLookup
        {
            public string Code { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string? Unit { get; set; }
        }

        private IReadOnlyCollection<string> GetModelErrors()
        {
            return ModelState
                .Where(entry => entry.Value?.Errors?.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? $"Invalid value for {entry.Key}"
                        : error.ErrorMessage))
                .ToArray();
        }

        public sealed class MeasurementResultDto
        {
            public int ResultID { get; set; }
            public string Type { get; set; } = string.Empty;
            public int EmissionSourceID { get; set; }
            public string EmissionSourceName { get; set; } = string.Empty;
            public string ParameterCode { get; set; } = string.Empty;
            public string ParameterName { get; set; } = string.Empty;
            public DateTime? MeasurementDate { get; set; }
            public double? Value { get; set; }
            public string? Unit { get; set; }
            public string? Remark { get; set; }
            public bool IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }

        public sealed class MeasurementResultRequest
        {
            public string? Type { get; set; }
            public int EmissionSourceId { get; set; }
            public string ParameterCode { get; set; } = string.Empty;
            public DateTime MeasurementDate { get; set; }
            public double? Value { get; set; }
            public string? Unit { get; set; }
            public bool IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string? Remark { get; set; }
        }

        private sealed class ParameterTrendResponse
        {
            public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
            public IReadOnlyList<ParameterTrendSeries> Series { get; init; } = Array.Empty<ParameterTrendSeries>();
        }

        private sealed class ParameterTrendSeries
        {
            public string ParameterCode { get; init; } = string.Empty;
            public string ParameterName { get; init; } = string.Empty;
            public string? Unit { get; init; }
            public IReadOnlyList<ParameterTrendPoint> Points { get; init; } = Array.Empty<ParameterTrendPoint>();
        }

        private sealed class ParameterTrendPoint
        {
            public string Month { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
            public double? Min { get; init; }
            public double? Max { get; init; }
        }

        private sealed class ParameterAggregate
        {
            public string ParameterCode { get; init; } = string.Empty;
            public int Year { get; init; }
            public int Month { get; init; }
            public double? Min { get; init; }
            public double? Max { get; init; }
        }

        private static string CreateAggregateKey(string code, int year, int month) =>
            $"{code?.Trim().ToUpperInvariant()}:{year:D4}:{month:D2}";
    }
}
