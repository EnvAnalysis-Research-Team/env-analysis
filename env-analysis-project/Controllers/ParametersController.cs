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

namespace env_analysis_project.Controllers
{
    public class ParametersController : Controller
    {
        private readonly env_analysis_projectContext _context;

        public ParametersController(env_analysis_projectContext context)
        {
            _context = context;
        }

        // GET: Parameters
        public async Task<IActionResult> Index()
        {
            return View(await _context.Parameter.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.ParameterCode == id);
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
        public async Task<IActionResult> Create([Bind("ParameterCode,ParameterName,Unit,StandardValue,Description,CreatedAt,UpdatedAt")] Parameter parameter)
        {
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
            parameter.CreatedAt = DateTime.UtcNow;
            parameter.UpdatedAt = DateTime.UtcNow;

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
        public async Task<IActionResult> Edit(string id, [Bind("ParameterCode,ParameterName,Unit,StandardValue,Description,CreatedAt,UpdatedAt")] Parameter parameter)
        {
            if (id != parameter.ParameterCode)
            {
                return NotFound();
            }

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
                .FirstOrDefaultAsync(m => m.ParameterCode == id);
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
            if (parameter != null)
            {
                _context.Parameter.Remove(parameter);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============================
        //  AJAX ENDPOINTS
        // =============================
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var parameters = await _context.Parameter
                .OrderBy(p => p.ParameterName)
                .Select(p => ToDto(p))
                .ToListAsync();

            return Ok(ApiResponse.Success(parameters));
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
            if (parameter == null)
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
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim(),
                StandardValue = dto.StandardValue,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Parameter.Add(entity);
            await _context.SaveChangesAsync();

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
            if (parameter == null)
            {
                return NotFound(ApiResponse.Fail<ParameterDto>("Parameter not found."));
            }

            var validationErrors = ParameterValidator.ValidateDto(dto, isUpdate: true).ToList();
            if (validationErrors.Count > 0)
            {
                return BadRequest(ApiResponse.Fail<ParameterDto>("Validation failed.", validationErrors));
            }

            parameter.ParameterName = dto!.ParameterName.Trim();
            parameter.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();
            parameter.StandardValue = dto.StandardValue;
            parameter.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            parameter.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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

            _context.Parameter.Remove(parameter);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success<object?>(null, "Parameter deleted successfully."));
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
            return _context.Parameter.Any(e => e.ParameterCode == id);
        }

        private static ParameterDto ToDto(Parameter parameter)
        {
            return new ParameterDto
            {
                ParameterCode = parameter.ParameterCode,
                ParameterName = parameter.ParameterName,
                Unit = parameter.Unit,
                StandardValue = parameter.StandardValue,
                Description = parameter.Description,
                CreatedAt = parameter.CreatedAt,
                UpdatedAt = parameter.UpdatedAt
            };
        }

        public sealed class ParameterDto
        {
            public string ParameterCode { get; set; } = string.Empty;
            public string ParameterName { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public double? StandardValue { get; set; }
            public string? Description { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
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
