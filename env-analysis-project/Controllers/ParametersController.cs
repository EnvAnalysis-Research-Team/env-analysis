using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Data;
using env_analysis_project.Models;
using env_analysis_project.Validators;
using env_analysis_project.Services;

namespace env_analysis_project.Controllers
{
    public class ParametersController : Controller
    {
        private readonly env_analysis_projectContext _context;
        private readonly IUserActivityLogger _activityLogger;

        public ParametersController(env_analysis_projectContext context, IUserActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Parameters
        public async Task<IActionResult> Index()
        {
            var parameters = await _context.Parameter
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            return View(parameters);
        }

        // GET: Parameters/Manage
        [HttpGet]
        public IActionResult Manage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var parameters = await _context.Parameter
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ParameterName)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Code,Name,Unit,Standard,Description,Created");
            foreach (var parameter in parameters)
            {
                var fields = new[]
                {
                    EscapeCsv(parameter.ParameterCode),
                    EscapeCsv(parameter.ParameterName),
                    EscapeCsv(parameter.Unit),
                    EscapeCsv(parameter.StandardValue?.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(parameter.Description),
                    EscapeCsv(parameter.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                    //EscapeCsv(parameter.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                };
                builder.AppendLine(string.Join(",", fields));
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var fileName = $"parameters-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // GET: Parameters/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parameter = await _context.Parameter
                .FirstOrDefaultAsync(m => m.ParameterCode == id && !m.IsDeleted);
            if (parameter == null)
            {
                return NotFound();
            }

            return View(parameter);
        }

        // GET: Parameters/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Parameters/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ParameterCode,ParameterName,Type,Unit,StandardValue,Description,CreatedAt,UpdatedAt")] Parameter parameter)
        {
            parameter.Type = ParameterTypeHelper.Normalize(parameter.Type);
            var validationErrors = ParameterValidator.Validate(parameter).ToList();
            if (!ModelState.IsValid)
            {
                validationErrors.AddRange(GetModelErrors());
            }

            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View(parameter);
            }

            parameter.ParameterCode = parameter.ParameterCode.Trim();
            parameter.ParameterName = parameter.ParameterName.Trim();
            parameter.Unit = string.IsNullOrWhiteSpace(parameter.Unit) ? null : parameter.Unit.Trim();
            parameter.Description = string.IsNullOrWhiteSpace(parameter.Description) ? null : parameter.Description.Trim();
            parameter.Type = ParameterTypeHelper.Normalize(parameter.Type);
            parameter.CreatedAt = DateTime.UtcNow;
            parameter.UpdatedAt = DateTime.UtcNow;
            parameter.IsDeleted = false;

            _context.Add(parameter);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Parameters/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parameter = await _context.Parameter.FindAsync(id);
            if (parameter == null)
            {
                return NotFound();
            }
            return View(parameter);
        }

        // POST: Parameters/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ParameterCode,ParameterName,Type,Unit,StandardValue,Description,CreatedAt,UpdatedAt")] Parameter parameter)
        {
            if (id != parameter.ParameterCode)
            {
                return NotFound();
            }
            parameter.Type = ParameterTypeHelper.Normalize(parameter.Type);

            var validationErrors = ParameterValidator.Validate(parameter).ToList();
            if (!ModelState.IsValid)
            {
                validationErrors.AddRange(GetModelErrors());
            }

            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View(parameter);
            }

            try
            {
                parameter.ParameterName = parameter.ParameterName.Trim();
                parameter.Unit = string.IsNullOrWhiteSpace(parameter.Unit) ? null : parameter.Unit.Trim();
                parameter.Description = string.IsNullOrWhiteSpace(parameter.Description) ? null : parameter.Description.Trim();
                parameter.Type = ParameterTypeHelper.Normalize(parameter.Type);
                parameter.UpdatedAt = DateTime.UtcNow;

                _context.Update(parameter);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParameterExists(parameter.ParameterCode))
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

        // GET: Parameters/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var parameter = await _context.Parameter
                .FirstOrDefaultAsync(m => m.ParameterCode == id && !m.IsDeleted);
            if (parameter == null)
            {
                return NotFound();
            }

            return View(parameter);
        }

        // POST: Parameters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var parameter = await _context.Parameter.FindAsync(id);
            if (parameter != null && !parameter.IsDeleted)
            {
                parameter.IsDeleted = true;
                parameter.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =============================
        //  AJAX ENDPOINTS
        // =============================
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var parameters = await _context.Parameter
                .OrderBy(p => p.IsDeleted)
                .ThenBy(p => p.ParameterName)
                .Select(p => ToDto(p))
                .ToListAsync();

            return Ok(ApiResponse.Success(parameters));
        }

        [HttpGet]
        public async Task<IActionResult> LatestMeasurementValues()
        {
            const string sql = """
WITH LatestMeasurement AS (
    SELECT
        mr.ResultID,
        mr.ParameterCode,
        mr.MeasurementDate,
        mr.EntryDate,
        mr.Value,
        mr.Unit,
        mr.EmissionSourceID,
        p.ParameterName,
        p.StandardValue,
        p.Type,
        ROW_NUMBER() OVER (
            PARTITION BY mr.ParameterCode
            ORDER BY
                mr.MeasurementDate DESC,
                mr.EntryDate DESC,
                mr.ResultID DESC
        ) AS rn
    FROM MeasurementResult mr
    INNER JOIN Parameter p
        ON mr.ParameterCode = p.ParameterCode
    WHERE p.IsDeleted = 0
)
SELECT
    ResultID,
    ParameterCode,
    MeasurementDate,
    EntryDate,
    Value,
    Unit,
    EmissionSourceID,
    ParameterName,
    StandardValue,
    Type
FROM LatestMeasurement
WHERE rn = 1
ORDER BY ParameterName;
""";

            var records = await _context.Set<LatestParameterMeasurementRecord>()
                .FromSqlRaw(sql)
                .AsNoTracking()
                .ToListAsync();

            var latestMeasurements = records
                .Select(record => new LatestParameterMeasurementDto
                {
                    ParameterCode = record.ParameterCode,
                    ParameterName = record.ParameterName,
                    Unit = record.Unit,
                    Value = record.Value,
                    MeasurementDate = record.MeasurementDate == default(DateTime)
                        ? record.EntryDate
                        : record.MeasurementDate
                })
                .OrderBy(dto => dto.ParameterName)
                .ToList();

            return Ok(ApiResponse.Success(latestMeasurements));
        }

        [HttpGet]
        public async Task<IActionResult> DetailData(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse.Fail<ParameterDto>("Parameter code is required."));
            }

            var code = id.Trim();
            var parameter = await _context.Parameter.FindAsync(code);
            if (parameter == null || parameter.IsDeleted)
            {
                return NotFound(ApiResponse.Fail<ParameterDto>("Parameter not found."));
            }

            return Ok(ApiResponse.Success(ToDto(parameter)));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] ParameterDto dto)
        {
            var validationErrors = ParameterValidator.ValidateDto(dto).ToList();
            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<ParameterDto>("Validation failed.", validationErrors));
            }

            var code = dto!.ParameterCode.Trim();
            if (await _context.Parameter.AnyAsync(p => p.ParameterCode == code))
            {
                return Conflict(ApiResponse.Fail<ParameterDto>($"Parameter with code '{code}' already exists."));
            }

            var entity = new Parameter
            {
                ParameterCode = code,
                ParameterName = dto.ParameterName.Trim(),
                Type = ParameterTypeHelper.Normalize(dto.Type),
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim(),
                StandardValue = dto.StandardValue,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Parameter.Add(entity);
            await _context.SaveChangesAsync();
            await LogAsync("Parameter.Create", entity.ParameterCode, $"Created parameter {entity.ParameterName}");
            return Ok(ApiResponse.Success(ToDto(entity), "Parameter created successfully."));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAjax(string id, [FromBody] ParameterDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse.Fail<ParameterDto>("Parameter code is required."));
            }

            var code = id.Trim();
            var parameter = await _context.Parameter.FindAsync(code);
            if (parameter == null || parameter.IsDeleted)
            {
                return NotFound(ApiResponse.Fail<ParameterDto>("Parameter not found."));
            }

            var validationErrors = ParameterValidator.ValidateDto(dto, isUpdate: true).ToList();
            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<ParameterDto>("Validation failed.", validationErrors));
            }

            parameter.ParameterName = dto!.ParameterName.Trim();
            parameter.Type = ParameterTypeHelper.Normalize(dto.Type);
            parameter.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();
            parameter.StandardValue = dto.StandardValue;
            parameter.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            parameter.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogAsync("Parameter.Update", parameter.ParameterCode, $"Updated parameter {parameter.ParameterName}");
            return Ok(ApiResponse.Success(ToDto(parameter), "Parameter updated successfully."));
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAjax(string id)
        {
            var validationErrors = ParameterValidator.ValidateIdentifier(id).ToList();
            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<object?>(validationErrors[0], validationErrors));
            }

            var parameter = await _context.Parameter.FindAsync(id.Trim());
            if (parameter == null)
            {
                return NotFound(ApiResponse.Fail<object?>("Parameter not found."));
            }

            if (parameter.IsDeleted)
            {
                return BadRequest(ApiResponse.Fail<object?>("Parameter is already deleted."));
            }

            parameter.IsDeleted = true;
            parameter.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogAsync("Parameter.Delete", parameter.ParameterCode, $"Deleted parameter {parameter.ParameterName}");
            return Ok(ApiResponse.Success(ToDto(parameter), "Parameter deleted successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreAjax([FromBody] ParameterDto request)
        {
            var validationErrors = ParameterValidator.ValidateIdentifier(request?.ParameterCode).ToList();
            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<object?>(validationErrors[0], validationErrors));
            }

            var parameter = await _context.Parameter.FindAsync(request!.ParameterCode.Trim());
            if (parameter == null)
            {
                return NotFound(ApiResponse.Fail<object?>("Parameter not found."));
            }

            if (!parameter.IsDeleted)
            {
                return BadRequest(ApiResponse.Fail<object?>("Parameter is already active."));
            }

            parameter.IsDeleted = false;
            parameter.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogAsync("Parameter.Restore", parameter.ParameterCode, $"Restored parameter {parameter.ParameterName}");
            return Ok(ApiResponse.Success(ToDto(parameter), "Parameter restored successfully."));
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

        private bool ParameterExists(string id)
        {
            return _context.Parameter.Any(e => e.ParameterCode == id && !e.IsDeleted);
        }

        private static ParameterDto ToDto(Parameter parameter)
        {
            return new ParameterDto
            {
                ParameterCode = parameter.ParameterCode,
                ParameterName = parameter.ParameterName,
                Type = parameter.Type,
                Unit = parameter.Unit,
                StandardValue = parameter.StandardValue,
                Description = parameter.Description,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt,
                IsDeleted = parameter.IsDeleted
            };
        }

        public sealed class ParameterDto
        {
            public string ParameterCode { get; set; } = string.Empty;
            public string ParameterName { get; set; } = string.Empty;
            public string Type { get; set; } = "water";
            public string? Unit { get; set; }
            public double? StandardValue { get; set; }
            public string? Description { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
        }

        private sealed class LatestParameterMeasurementDto
        {
            public string ParameterCode { get; init; } = string.Empty;
            public string ParameterName { get; init; } = string.Empty;
            public string? Unit { get; init; }
            public double? Value { get; init; }
            public DateTime? MeasurementDate { get; init; }
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

        private Task LogAsync(string action, string entityId, string description) =>
            _activityLogger.LogAsync(action, "Parameter", entityId, description);
    }
}
