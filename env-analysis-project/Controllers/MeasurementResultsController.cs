using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;
using env_analysis_project.Validators;
using env_analysis_project.Services;

namespace env_analysis_project.Controllers
{
    public class MeasurementResultsController : Controller
    {
        private readonly env_analysis_projectContext _context;
        private readonly IUserActivityLogger _activityLogger;
        private readonly IMeasurementImportService _measurementImportService;

        public MeasurementResultsController(
            env_analysis_projectContext context,
            IUserActivityLogger activityLogger,
            IMeasurementImportService measurementImportService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _measurementImportService = measurementImportService;
        }

        public async Task<IActionResult> Manage()
        {
            var emissionSources = await _context.EmissionSource
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SourceName)
                .Select(s => new LookupOption
                {
                    Id = s.EmissionSourceID,
                    Label = s.SourceName
                })
                .ToListAsync();

            var parameters = await _context.Parameter
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ParameterName)
                .Select(p => new ParameterLookup
                {
                    Code = p.ParameterCode,
                    Label = p.ParameterName,
                    Unit = p.Unit,
                    Type = ParameterTypeHelper.Normalize(p.Type)
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
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource.Where(e => !e.IsDeleted), "EmissionSourceID", "SourceCode");
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>().Where(p => !p.IsDeleted), "ParameterCode", "ParameterCode");
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
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource.Where(e => !e.IsDeleted), "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>().Where(p => !p.IsDeleted), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
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
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource.Where(e => !e.IsDeleted), "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>().Where(p => !p.IsDeleted), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
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
            ViewData["EmissionSourceID"] = new SelectList(_context.EmissionSource.Where(e => !e.IsDeleted), "EmissionSourceID", "SourceCode", measurementResult.EmissionSourceID);
            ViewData["ParameterCode"] = new SelectList(_context.Set<Parameter>().Where(p => !p.IsDeleted), "ParameterCode", "ParameterCode", measurementResult.ParameterCode);
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
        public async Task<IActionResult> ListData(
            string? type,
            int page = 1,
            int pageSize = 10,
            bool paged = false,
            string? search = null,
            int? sourceId = null,
            string? parameterCode = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            const int DefaultPageSize = 10;
            const int MaxPageSize = 100;

            var normalizedType = NormalizeTypeFilter(type);
            var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var normalizedStatus = NormalizeStatusFilter(status);

            var filteredQuery = BuildFilteredQuery(
                normalizedType,
                trimmedSearch,
                sourceId,
                parameterCode,
                normalizedStatus,
                startDate,
                endDate);

            var totalItems = await filteredQuery.CountAsync();

            var effectivePageSize = paged
                ? Math.Min(Math.Max(pageSize, 1), MaxPageSize)
                : (totalItems == 0 ? DefaultPageSize : totalItems);

            var totalPages = Math.Max(1, (int)Math.Ceiling((double)Math.Max(totalItems, 0) / Math.Max(effectivePageSize, 1)));
            var currentPage = paged ? Math.Min(Math.Max(page, 1), totalPages) : 1;

            var orderedQuery = filteredQuery
                .OrderByDescending(m => m.MeasurementDate)
                .ThenByDescending(m => m.ResultID);

            var pagedQuery = paged
                ? orderedQuery.Skip((currentPage - 1) * effectivePageSize).Take(effectivePageSize)
                : orderedQuery;

            var items = await pagedQuery
                .Select(m => ToDto(m))
                .ToListAsync();

            var pagination = new PaginationMetadata
            {
                Page = paged ? currentPage : 1,
                PageSize = paged ? effectivePageSize : items.Count,
                TotalItems = totalItems,
                TotalPages = paged ? totalPages : 1
            };

            var response = new MeasurementResultListResponse
            {
                Items = items,
                Pagination = pagination,
                Summary = await BuildSummaryAsync()
            };

            return Ok(ApiResponse.Success(response));
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string? type,
            string? search = null,
            int? sourceId = null,
            string? parameterCode = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var normalizedType = NormalizeTypeFilter(type);
            var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search?.Trim();
            var normalizedStatus = NormalizeStatusFilter(status);

            var filteredQuery = BuildFilteredQuery(
                normalizedType,
                trimmedSearch,
                sourceId,
                parameterCode,
                normalizedStatus,
                startDate,
                endDate);

            var orderedQuery = filteredQuery
                .OrderByDescending(m => m.MeasurementDate)
                .ThenByDescending(m => m.ResultID);

            var entities = await orderedQuery.ToListAsync();
            var dtos = entities.Select(ToDto).ToList();
            var csv = BuildCsv(dtos);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var fileName = $"measurement-results-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPreview(IFormFile? file, [FromForm] int? emissionSourceId)
        {
            var result = await _measurementImportService.PreviewAsync(file, emissionSourceId);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(ApiResponse.Fail<MeasurementImportPreviewResponse>(
                    result.Message ?? "Unable to preview the import file.",
                    result.Errors));
            }

            return Ok(ApiResponse.Success(result.Data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportConfirm([FromBody] MeasurementImportConfirmRequest request)
        {
            var result = await _measurementImportService.ConfirmAsync(request);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(ApiResponse.Fail<MeasurementImportConfirmResponse>(
                    result.Message ?? "Unable to import measurement results.",
                    result.Errors));
            }

            return Ok(ApiResponse.Success(result.Data, result.Message));
        }

        [HttpGet]
        public async Task<IActionResult> ParameterTrends(
            [FromQuery] string? code,
            [FromQuery(Name = "codes")] string[]? codes,
            [FromQuery] string? startMonth,
            [FromQuery] string? endMonth,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] int? sourceId = null)
        {
            var normalizedCodes = new List<string>();
            if (codes != null)
            {
                foreach (var entry in codes)
                {
                    if (!string.IsNullOrWhiteSpace(entry))
                    {
                        normalizedCodes.Add(entry.Trim().ToUpperInvariant());
                    }
                }
            }

            if (normalizedCodes.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("Parameter code is required."));
                }
                normalizedCodes.Add(code.Trim().ToUpperInvariant());
            }

            normalizedCodes = normalizedCodes.Distinct().ToList();
            var isMultiParameterRequest = normalizedCodes.Count > 1;

            const int MaxMonths = 36;
            const int DefaultTablePageSize = 12;
            const int AlternateTablePageSize = 6;
            var now = DateTime.UtcNow;
            var defaultEnd = new DateTime(now.Year, now.Month, 1);
            var defaultStart = defaultEnd.AddMonths(-11);

            DateTime? parsedStart = null;
            DateTime? parsedEnd = null;

            if (!string.IsNullOrWhiteSpace(startMonth))
            {
                if (!TryParseMonth(startMonth, out var tmpStart))
                {
                    return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("Invalid start month format. Use yyyy-MM."));
                }
                parsedStart = tmpStart;
            }

            if (!string.IsNullOrWhiteSpace(endMonth))
            {
                if (!TryParseMonth(endMonth, out var tmpEnd))
                {
                    return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("Invalid end month format. Use yyyy-MM."));
                }
                parsedEnd = tmpEnd;
            }

            var rangeStart = new DateTime((parsedStart ?? parsedEnd ?? defaultStart).Year, (parsedStart ?? parsedEnd ?? defaultStart).Month, 1);
            var rangeEnd = new DateTime((parsedEnd ?? parsedStart ?? defaultEnd).Year, (parsedEnd ?? parsedStart ?? defaultEnd).Month, 1);

            if (rangeEnd < rangeStart)
            {
                return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("End month must be greater than or equal to start month."));
            }

            var months = new List<DateTime>();
            var cursor = rangeStart;
            while (cursor <= rangeEnd && months.Count < MaxMonths)
            {
                months.Add(cursor);
                cursor = cursor.AddMonths(1);
            }

            if (months.Count == 0)
            {
                months.Add(rangeStart);
            }

            var effectiveStart = months.First();
            var effectiveEndExclusive = months.Last().AddMonths(1);

            var metadataEntries = await _context.Parameter
                .Where(p => normalizedCodes.Contains(p.ParameterCode.ToUpper()) && !p.IsDeleted)
                .Select(p => new ParameterLookup
                {
                    Code = p.ParameterCode,
                    Label = p.ParameterName,
                    Unit = p.Unit,
                    Type = ParameterTypeHelper.Normalize(p.Type)
                })
                .ToListAsync();

            if (metadataEntries.Count != normalizedCodes.Count)
            {
                return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("One or more parameters were not found."));
            }

            if (isMultiParameterRequest && metadataEntries.Any(entry => entry.Type != "water"))
            {
                return BadRequest(ApiResponse.Fail<ParameterTrendResponse>("Multi-parameter trends are only available for water parameters."));
            }

            var metadataLookup = metadataEntries
                .ToDictionary(entry => entry.Code.ToUpperInvariant(), entry => entry);

            var normalizedCodeSet = normalizedCodes.ToHashSet();

            var measurementQuery = _context.MeasurementResult
                .AsNoTracking()
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .Where(m => m.ParameterCode != null &&
                            normalizedCodeSet.Contains(m.ParameterCode.ToUpper()) &&
                            m.Value.HasValue);

            if (sourceId.HasValue)
            {
                measurementQuery = measurementQuery.Where(m => m.EmissionSourceID == sourceId.Value);
            }

            var measurements = await measurementQuery
                .Select(m => new
                {
                    CodeUpper = m.ParameterCode.ToUpper(),
                    Date = m.MeasurementDate == default ? m.EntryDate : m.MeasurementDate,
                    m.Value,
                    SourceName = m.EmissionSource != null ? m.EmissionSource.SourceName : null,
                    ParameterName = m.Parameter != null ? m.Parameter.ParameterName : m.ParameterCode
                })
                .Where(x => x.Date >= effectiveStart && x.Date < effectiveEndExclusive)
                .OrderBy(x => x.Date)
                .ToListAsync();

            var orderedMeasurements = measurements.ToList();
            var uniqueDates = orderedMeasurements
                .Select(entry => entry.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var labels = uniqueDates
                .Select(date => date.ToString("dd MMM yyyy HH:mm"))
                .ToArray();

            IReadOnlyList<ParameterTrendSeries> series;
            List<ParameterTrendPoint> tablePoints;

            if (isMultiParameterRequest)
            {
                var groupedParameters = orderedMeasurements
                    .GroupBy(entry => entry.CodeUpper)
                    .OrderBy(group => metadataLookup[group.Key].Label, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                series = groupedParameters.Select(group =>
                {
                    var meta = metadataLookup[group.Key];
                    var valueByDate = group
                        .GroupBy(item => item.Date)
                        .ToDictionary(g => g.Key, g => g.Average(x => x.Value));

                    var seriesPoints = uniqueDates.Select(date => new ParameterTrendPoint
                    {
                        Month = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        Label = date.ToString("dd MMM yyyy HH:mm"),
                        Value = valueByDate.TryGetValue(date, out var value) ? value : null,
                        ParameterName = meta.Label,
                        Unit = meta.Unit
                    }).ToList();

                    return new ParameterTrendSeries
                    {
                        ParameterCode = meta.Code,
                        ParameterName = meta.Label,
                        Unit = meta.Unit,
                        Points = seriesPoints
                    };
                }).ToArray();

                var aggregatedSourceLabel = sourceId.HasValue
                    ? orderedMeasurements.FirstOrDefault(entry => !string.IsNullOrWhiteSpace(entry.SourceName))?.SourceName ?? $"Source #{sourceId.Value}"
                    : "All sources";

                tablePoints = groupedParameters
                    .SelectMany(group =>
                    {
                        var meta = metadataLookup[group.Key];
                        return group
                            .GroupBy(entry => entry.Date)
                            .Select(g => new ParameterTrendPoint
                            {
                                Month = g.Key.ToString("yyyy-MM-ddTHH:mm:ss"),
                                Label = g.Key.ToString("dd MMM yyyy HH:mm"),
                                Value = g.Average(x => x.Value),
                                SourceName = aggregatedSourceLabel,
                                ParameterName = meta.Label,
                                Unit = meta.Unit
                            });
                    })
                    .OrderBy(point => point.ParameterName)
                    .ThenBy(point => point.Month)
                    .ToList();
            }
            else
            {
                var groupedSources = orderedMeasurements
                    .GroupBy(entry => string.IsNullOrWhiteSpace(entry.SourceName) ? "Unknown source" : entry.SourceName!)
                    .ToList();

                var metadata = metadataLookup[normalizedCodes[0]];

                series = groupedSources.Select(group =>
                {
                    var valueByDate = group
                        .GroupBy(item => item.Date)
                        .ToDictionary(g => g.Key, g => g.First().Value);

                    var seriesPoints = uniqueDates.Select(date => new ParameterTrendPoint
                    {
                        Month = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        Label = date.ToString("dd MMM yyyy HH:mm"),
                        Value = valueByDate.TryGetValue(date, out var value) ? value : null,
                        SourceName = group.Key,
                        ParameterName = metadata.Label,
                        Unit = metadata.Unit
                    }).ToList();

                    return new ParameterTrendSeries
                    {
                        ParameterCode = metadata.Code,
                        ParameterName = group.Key,
                        Unit = metadata.Unit,
                        Points = seriesPoints
                    };
                }).ToArray();

                tablePoints = orderedMeasurements.Select(entry => new ParameterTrendPoint
                {
                    Month = entry.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Label = entry.Date.ToString("dd MMM yyyy HH:mm"),
                    Value = entry.Value,
                    SourceName = string.IsNullOrWhiteSpace(entry.SourceName) ? "Unknown source" : entry.SourceName,
                    ParameterName = metadata.Label,
                    Unit = metadata.Unit
                }).ToList();
            }

            var defaultMetadata = metadataLookup.TryGetValue(normalizedCodes[0], out var firstMeta)
                ? firstMeta
                : null;

            var totalItems = tablePoints.Count;
            var normalizedPageSize = pageSize == AlternateTablePageSize ? AlternateTablePageSize : DefaultTablePageSize;
            if (normalizedPageSize <= 0)
            {
                normalizedPageSize = DefaultTablePageSize;
            }
            var totalPages = Math.Max(1, (int)Math.Ceiling(Math.Max(totalItems, 0) / (double)normalizedPageSize));
            var safePage = Math.Min(Math.Max(page, 1), totalPages);
            var tableItems = tablePoints
                .Skip((safePage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArray();

            var table = new TrendTablePage
            {
                Unit = !isMultiParameterRequest ? defaultMetadata?.Unit : null,
                Items = tableItems,
                Pagination = new PaginationMetadata
                {
                    Page = safePage,
                    PageSize = normalizedPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                },
                SourceId = sourceId
            };

            var response = new ParameterTrendResponse
            {
                Labels = labels,
                Series = series,
                Table = table
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

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId && !s.IsDeleted))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Emission source not found."));
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode && !p.IsDeleted))
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
                ApprovedAt = request.IsApproved ? request.ApprovedAt : null
            };

            _context.MeasurementResult.Add(entity);
            await _context.SaveChangesAsync();
            await LogAsync("MeasurementResult.Create", entity.ResultID.ToString(), $"Created measurement result for parameter {entity.ParameterCode}", new { entity.EmissionSourceID, entity.Value });

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

            request.MeasurementDate = entity.MeasurementDate;

            DateTime? computedApprovedAt;
            if (request.IsApproved)
            {
                computedApprovedAt = entity.ApprovedAt ?? DateTime.UtcNow;
            }
            else
            {
                computedApprovedAt = null;
            }
            request.ApprovedAt = computedApprovedAt;

            var validationErrors = MeasurementResultValidator.Validate(request).ToList();
            if (!ModelState.IsValid)
            {
                validationErrors.AddRange(GetModelErrors());
            }

            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Invalid measurement result payload.", validationErrors));
            }

            if (!await _context.EmissionSource.AnyAsync(s => s.EmissionSourceID == request.EmissionSourceId && !s.IsDeleted))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Emission source not found."));
            }

            if (!await _context.Parameter.AnyAsync(p => p.ParameterCode == request.ParameterCode && !p.IsDeleted))
            {
                return BadRequest(ApiResponse.Fail<MeasurementResultDto>("Parameter not found."));
            }

            entity.EmissionSourceID = request.EmissionSourceId;
            entity.ParameterCode = request.ParameterCode;
            entity.Value = request.Value;
            entity.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
            entity.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
            entity.IsApproved = request.IsApproved;
            entity.ApprovedAt = computedApprovedAt;

            await _context.SaveChangesAsync();
            await LogAsync("MeasurementResult.Update", entity.ResultID.ToString(), $"Updated measurement result for parameter {entity.ParameterCode}", new { entity.EmissionSourceID, entity.Value });

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
            await LogAsync("MeasurementResult.Delete", id.ToString(), $"Deleted measurement result for parameter {entity.ParameterCode}", new { entity.EmissionSourceID });

            return Ok(ApiResponse.Success<object?>(null, "Measurement result deleted successfully."));
        }

        private bool MeasurementResultExists(int id)
        {
            return _context.MeasurementResult.Any(e => e.ResultID == id);
        }

        private static string? NormalizeTypeFilter(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var trimmed = input.Trim();
            if (trimmed.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return ParameterTypeHelper.IsValid(trimmed)
                ? ParameterTypeHelper.Normalize(trimmed)
                : null;
        }

        private static string NormalizeType(string? input)
        {
            return ParameterTypeHelper.Normalize(input);
        }

        private static string? NormalizeStatusFilter(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var normalized = status.Trim().ToLowerInvariant();
            return normalized is "approved" or "pending" ? normalized : null;
        }

        private IQueryable<MeasurementResult> BuildFilteredQuery(
            string? normalizedType,
            string? trimmedSearch,
            int? sourceId,
            string? parameterCode,
            string? normalizedStatus,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.MeasurementResult
                .AsNoTracking()
                .Include(m => m.EmissionSource)
                .Include(m => m.Parameter)
                .AsQueryable();

            if (!string.IsNullOrEmpty(normalizedType))
            {
                query = query.Where(m => m.Parameter != null && m.Parameter.Type == normalizedType);
            }

            if (sourceId.HasValue)
            {
                query = query.Where(m => m.EmissionSourceID == sourceId.Value);
            }

            if (!string.IsNullOrWhiteSpace(parameterCode))
            {
                var normalizedParameter = parameterCode.Trim().ToUpperInvariant();
                query = query.Where(m => m.ParameterCode != null &&
                                         m.ParameterCode.ToUpper() == normalizedParameter);
            }

            if (!string.IsNullOrEmpty(normalizedStatus))
            {
                var isApprovedFilter = normalizedStatus == "approved";
                query = query.Where(m => m.IsApproved == isApprovedFilter);
            }

            if (startDate.HasValue)
            {
                var normalizedStart = startDate.Value.Date;
                query = query.Where(m => m.MeasurementDate >= normalizedStart);
            }

            if (endDate.HasValue)
            {
                var normalizedEndExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(m => m.MeasurementDate < normalizedEndExclusive);
            }

            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var likePattern = $"%{trimmedSearch}%";
                var normalizedSearch = trimmedSearch.ToLowerInvariant();
                var statusMatch = normalizedSearch switch
                {
                    "approved" => "approved",
                    "pending" => "pending",
                    _ => null
                };

                query = query.Where(m =>
                    (m.EmissionSource != null && EF.Functions.Like(m.EmissionSource.SourceName ?? string.Empty, likePattern)) ||
                    EF.Functions.Like(m.Parameter.ParameterName ?? string.Empty, likePattern) ||
                    EF.Functions.Like(m.ParameterCode ?? string.Empty, likePattern) ||
                    (statusMatch == "approved" && m.IsApproved) ||
                    (statusMatch == "pending" && !m.IsApproved));
            }

            return query;
        }

        private static MeasurementResultDto ToDto(MeasurementResult measurement)
        {
            return new MeasurementResultDto
            {
                ResultID = measurement.ResultID,
                Type = NormalizeType(measurement.Parameter?.Type),
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

        private static string BuildCsv(IEnumerable<MeasurementResultDto> items)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Type,Source,Parameter,Value,Unit,Measurement Date,Status,Approved At,Remark");
            foreach (var item in items)
            {
                var valueText = item.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var measurementDate = item.MeasurementDate?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
                var statusText = item.IsApproved ? "Approved" : "Pending";
                var approvedAt = item.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
                var remark = item.Remark ?? string.Empty;

                var columns = new[]
                {
                    EscapeCsv(item.Type),
                    EscapeCsv(item.EmissionSourceName),
                    EscapeCsv(item.ParameterName),
                    EscapeCsv(valueText),
                    EscapeCsv(item.Unit ?? string.Empty),
                    EscapeCsv(measurementDate),
                    EscapeCsv(statusText),
                    EscapeCsv(approvedAt),
                    EscapeCsv(remark)
                };

                builder.AppendLine(string.Join(",", columns));
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "\"\"";
            }

            var sanitized = input.Replace("\"", "\"\"");
            return $"\"{sanitized}\"";
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
            public string Type { get; set; } = "water";
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

        private async Task<MeasurementResultSummary> BuildSummaryAsync()
        {
            var typeCounts = await _context.MeasurementResult
                .AsNoTracking()
                .Include(m => m.Parameter)
                .GroupBy(m => m.Parameter != null ? m.Parameter.Type : null)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var summary = new MeasurementResultSummary();
            foreach (var entry in typeCounts)
            {
                summary.All += entry.Count;
                var normalized = NormalizeType(entry.Type);
                if (normalized == "air")
                {
                    summary.Air += entry.Count;
                }
                else
                {
                    summary.Water += entry.Count;
                }
            }

            return summary;
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
            public int EmissionSourceId { get; set; }
            public string ParameterCode { get; set; } = string.Empty;
            public DateTime MeasurementDate { get; set; }
            public double? Value { get; set; }
            public string? Unit { get; set; }
            public bool IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string? Remark { get; set; }
        }

        private sealed class MeasurementResultListResponse
        {
            public IReadOnlyList<MeasurementResultDto> Items { get; init; } = Array.Empty<MeasurementResultDto>();
            public PaginationMetadata Pagination { get; init; } = new PaginationMetadata();
            public MeasurementResultSummary Summary { get; init; } = new MeasurementResultSummary();
        }

        private sealed class PaginationMetadata
        {
            public int Page { get; init; }
            public int PageSize { get; init; }
            public int TotalItems { get; init; }
            public int TotalPages { get; init; }
        }

        private sealed class MeasurementResultSummary
        {
            public int All { get; set; }
            public int Water { get; set; }
            public int Air { get; set; }
        }

        private sealed class ParameterTrendResponse
        {
            public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
            public IReadOnlyList<ParameterTrendSeries> Series { get; init; } = Array.Empty<ParameterTrendSeries>();
            public TrendTablePage Table { get; init; } = new TrendTablePage();
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
            public double? Value { get; init; }
            public string? SourceName { get; init; }
            public string? ParameterName { get; init; }
            public string? Unit { get; init; }
        }


        private sealed class TrendTablePage
        {
            public IReadOnlyList<ParameterTrendPoint> Items { get; init; } = Array.Empty<ParameterTrendPoint>();
            public PaginationMetadata Pagination { get; init; } = new PaginationMetadata();
            public string? Unit { get; init; }
            public int? SourceId { get; init; }
        }

        private static bool TryParseMonth(string? input, out DateTime month)
        {
            month = default;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return DateTime.TryParseExact(
                input.Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out month);
        }

        private Task LogAsync(string action, string entityId, string description, object? metadata = null) =>
            _activityLogger.LogAsync(action, "MeasurementResult", entityId, description, metadata);
    }
}
