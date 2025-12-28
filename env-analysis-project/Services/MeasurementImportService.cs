using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using env_analysis_project.Data;
using env_analysis_project.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Validators;

namespace env_analysis_project.Services
{
    public interface IMeasurementImportService
    {
        Task<ServiceResult<MeasurementImportPreviewResponse>> PreviewAsync(IFormFile? file, int? emissionSourceId);
        Task<ServiceResult<MeasurementImportConfirmResponse>> ConfirmAsync(MeasurementImportConfirmRequest request);
    }

    public sealed class MeasurementImportService : IMeasurementImportService
    {
        private readonly env_analysis_projectContext _context;
        private readonly IUserActivityLogger _activityLogger;

        public MeasurementImportService(env_analysis_projectContext context, IUserActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<MeasurementImportPreviewResponse>> PreviewAsync(IFormFile? file, int? emissionSourceId)
        {
            if (file == null || file.Length == 0)
            {
                return ServiceResult<MeasurementImportPreviewResponse>.Fail("Please choose an Excel file to import.");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail("The uploaded file does not contain any worksheets.");
                }

                var headerRow = worksheet.FirstRowUsed();
                if (headerRow == null)
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail("The uploaded file is missing a header row.");
                }

                var headerMap = BuildImportHeaderMap(headerRow);
                if (!headerMap.ContainsKey("ParameterCode") ||
                    !headerMap.ContainsKey("Value") ||
                    (!headerMap.ContainsKey("MeasurementDate") && !headerMap.ContainsKey("EntryDate")))
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail(
                        "The Excel file must include ParameterCode, Value, and either MeasurementDate or EntryDate columns.");
                }

                var emissionSources = await _context.EmissionSource
                    .Where(e => !e.IsDeleted)
                    .Select(e => new { e.EmissionSourceID, e.SourceName })
                    .ToDictionaryAsync(e => e.EmissionSourceID, e => e.SourceName);

                if (!headerMap.ContainsKey("EmissionSourceId") && !emissionSourceId.HasValue)
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail(
                        "Select an emission source or include an EmissionSourceID column in the Excel file.");
                }

                if (emissionSourceId.HasValue && !emissionSources.ContainsKey(emissionSourceId.Value))
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail(
                        $"Emission source #{emissionSourceId.Value} was not found.");
                }

                var parameters = await _context.Parameter
                    .Where(p => !p.IsDeleted)
                    .Select(p => new ParameterLookup
                    {
                        Code = (p.ParameterCode ?? string.Empty).ToUpper(),
                        Label = p.ParameterName,
                        Unit = p.Unit,
                        Type = ParameterTypeHelper.Normalize(p.Type)
                    })
                    .ToDictionaryAsync(p => p.Code, p => p);

                var previews = new List<MeasurementImportRowPreview>();
                var headerRowNumber = headerRow.RowNumber();
                var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? headerRowNumber;

                for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
                {
                    var row = worksheet.Row(rowNumber);
                    if (row == null || IsRowCompletelyEmpty(row, headerMap.Values))
                    {
                        continue;
                    }

                    var input = ParseImportRow(row, rowNumber, headerMap);
                    if (input == null)
                    {
                        continue;
                    }

                    if (!input.EmissionSourceId.HasValue && emissionSourceId.HasValue)
                    {
                        input.EmissionSourceId = emissionSourceId.Value;
                    }

                    var preview = ValidateImportRow(input, emissionSources, parameters);
                    previews.Add(preview);
                }

                if (previews.Count == 0)
                {
                    return ServiceResult<MeasurementImportPreviewResponse>.Fail(
                        "The uploaded file does not contain any readable data rows.");
                }

                var response = new MeasurementImportPreviewResponse
                {
                    Rows = previews,
                    TotalRows = previews.Count,
                    ValidRows = previews.Count(r => r.IsValid),
                    InvalidRows = previews.Count(r => !r.IsValid)
                };

                return ServiceResult<MeasurementImportPreviewResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<MeasurementImportPreviewResponse>.Fail($"Unable to read the Excel file. {ex.Message}");
            }
        }

        public async Task<ServiceResult<MeasurementImportConfirmResponse>> ConfirmAsync(MeasurementImportConfirmRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
            {
                return ServiceResult<MeasurementImportConfirmResponse>.Fail("No rows were supplied for import.");
            }

            var emissionSources = await _context.EmissionSource
                .Where(e => !e.IsDeleted)
                .Select(e => new { e.EmissionSourceID, e.SourceName })
                .ToDictionaryAsync(e => e.EmissionSourceID, e => e.SourceName);

            var parameters = await _context.Parameter
                .Where(p => !p.IsDeleted)
                .Select(p => new ParameterLookup
                {
                    Code = (p.ParameterCode ?? string.Empty).ToUpper(),
                    Label = p.ParameterName,
                    Unit = p.Unit,
                    Type = ParameterTypeHelper.Normalize(p.Type)
                })
                .ToDictionaryAsync(p => p.Code, p => p);

            var previews = new List<MeasurementImportRowPreview>();
            foreach (var row in request.Rows)
            {
                var input = new MeasurementImportRowInput
                {
                    RowNumber = row.RowNumber,
                    EmissionSourceId = row.EmissionSourceId,
                    ParameterCode = row.ParameterCode,
                    MeasurementDate = row.MeasurementDate,
                    EntryDate = row.EntryDate,
                    Value = row.Value,
                    Unit = row.Unit,
                    Remark = row.Remark,
                    IsApproved = row.IsApproved,
                    ApprovedAt = row.ApprovedAt
                };

                var preview = ValidateImportRow(input, emissionSources, parameters);
                previews.Add(preview);
            }

            var validRows = previews.Where(row => row.IsValid).ToList();
            if (validRows.Count > 0)
            {
                foreach (var row in validRows)
                {
                    var entity = new MeasurementResult
                    {
                        EmissionSourceID = row.EmissionSourceId!.Value,
                        ParameterCode = row.ParameterCode!,
                        MeasurementDate = row.MeasurementDate ?? DateTime.UtcNow,
                        Value = row.Value,
                        Unit = row.Unit,
                        EntryDate = row.EntryDate ?? row.MeasurementDate ?? DateTime.UtcNow,
                        Remark = row.Remark,
                        IsApproved = row.IsApproved,
                        ApprovedAt = row.IsApproved ? row.ApprovedAt ?? DateTime.UtcNow : null
                    };

                    _context.MeasurementResult.Add(entity);
                }

                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync(
                    "MeasurementResult.Import",
                    "MeasurementResult",
                    "bulk",
                    $"Imported {validRows.Count} measurement results.",
                    new { inserted = validRows.Count, total = previews.Count });
            }

            var response = new MeasurementImportConfirmResponse
            {
                TotalRows = previews.Count,
                InsertedRows = validRows.Count,
                FailedRows = previews.Count - validRows.Count,
                Rows = previews
            };

            var message = validRows.Count > 0
                ? $"Imported {validRows.Count} of {previews.Count} rows."
                : "No measurement results were imported because all rows contain validation errors.";

            return ServiceResult<MeasurementImportConfirmResponse>.Ok(response, message);
        }

        private static Dictionary<string, int> BuildImportHeaderMap(IXLRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var normalized = NormalizeImportHeader(cell?.GetString());
                if (!string.IsNullOrEmpty(normalized))
                {
                    map[normalized] = cell.Address.ColumnNumber;
                }
            }

            return map;
        }

        private static string? NormalizeImportHeader(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var sanitized = new string(input.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            return sanitized switch
            {
                "emissionsource" or "emissionsourceid" or "sourceid" or "source" => "EmissionSourceId",
                "parameter" or "parametercode" => "ParameterCode",
                "measurement" or "measurementdate" or "measurementdatetime" => "MeasurementDate",
                "entrydate" or "entrydatetime" => "EntryDate",
                "value" => "Value",
                "unit" => "Unit",
                "remark" or "remarks" or "note" or "notes" => "Remark",
                "isapproved" or "approved" or "approval" => "IsApproved",
                "approvedat" or "approvaldate" or "approveddate" => "ApprovedAt",
                _ => null
            };
        }

        private static bool IsRowCompletelyEmpty(IXLRow row, IEnumerable<int> columns)
        {
            var unique = columns?.Distinct().ToArray() ?? Array.Empty<int>();
            if (unique.Length == 0)
            {
                return row.IsEmpty();
            }

            foreach (var column in unique)
            {
                var cell = row.Cell(column);
                if (cell != null && !cell.IsEmpty() && !string.IsNullOrWhiteSpace(cell.GetString()))
                {
                    return false;
                }
            }

            return true;
        }

        private static MeasurementImportRowInput? ParseImportRow(IXLRow row, int rowNumber, IReadOnlyDictionary<string, int> headerMap)
        {
            var input = new MeasurementImportRowInput { RowNumber = rowNumber };
            var hasValue = false;

            if (TryGetIntValue(row, headerMap, "EmissionSourceId", out var sourceId, out var sourceProvided))
            {
                input.EmissionSourceId = sourceId;
                hasValue = true;
            }
            else if (sourceProvided)
            {
                input.Errors.Add("Emission Source ID is invalid.");
                hasValue = true;
            }

            if (TryGetStringValue(row, headerMap, "ParameterCode", out var parameterCode))
            {
                input.ParameterCode = parameterCode;
                hasValue = true;
            }

            if (TryGetDateValue(row, headerMap, "MeasurementDate", out var measurementDate, out var measurementProvided))
            {
                input.MeasurementDate = measurementDate;
                hasValue = true;
            }
            else if (measurementProvided)
            {
                input.Errors.Add("Measurement date value is invalid.");
                hasValue = true;
            }

            if (TryGetDateValue(row, headerMap, "EntryDate", out var entryDate, out var entryProvided))
            {
                input.EntryDate = entryDate;
                hasValue = true;
            }
            else if (entryProvided)
            {
                input.Errors.Add("Entry date value is invalid.");
                hasValue = true;
            }

            if (TryGetDoubleValue(row, headerMap, "Value", out var value, out var valueProvided))
            {
                input.Value = value;
                hasValue = true;
            }
            else if (valueProvided)
            {
                input.Errors.Add("Value is invalid.");
                hasValue = true;
            }

            if (TryGetStringValue(row, headerMap, "Unit", out var unit))
            {
                input.Unit = unit;
                hasValue = true;
            }

            if (TryGetStringValue(row, headerMap, "Remark", out var remark))
            {
                input.Remark = remark;
                hasValue = true;
            }

            if (TryGetBooleanValue(row, headerMap, "IsApproved", out var isApproved, out var approvalProvided))
            {
                input.IsApproved = isApproved;
                hasValue = true;
            }
            else if (approvalProvided)
            {
                input.Errors.Add("IsApproved value is invalid.");
                hasValue = true;
            }

            if (TryGetDateValue(row, headerMap, "ApprovedAt", out var approvedAt, out var approvedProvided))
            {
                input.ApprovedAt = approvedAt;
                hasValue = true;
            }
            else if (approvedProvided)
            {
                input.Errors.Add("Approved At value is invalid.");
                hasValue = true;
            }

            return hasValue ? input : null;
        }

        private static bool TryGetStringValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key, out string? value)
        {
            value = null;
            if (!headerMap.TryGetValue(key, out var column))
            {
                return false;
            }

            var cell = row.Cell(column);
            if (cell == null || cell.IsEmpty())
            {
                return false;
            }

            var text = cell.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            value = text.Trim();
            return true;
        }

        private static bool TryGetIntValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key, out int value, out bool hasRawValue)
        {
            value = 0;
            hasRawValue = false;
            if (!headerMap.TryGetValue(key, out var column))
            {
                return false;
            }

            var cell = row.Cell(column);
            if (cell == null || cell.IsEmpty())
            {
                return false;
            }

            hasRawValue = true;
            if (cell.TryGetValue(out double numeric))
            {
                value = (int)Math.Round(numeric);
                return true;
            }

            var text = cell.GetString();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
                int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private static bool TryGetDoubleValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key, out double value, out bool hasRawValue)
        {
            value = 0;
            hasRawValue = false;
            if (!headerMap.TryGetValue(key, out var column))
            {
                return false;
            }

            var cell = row.Cell(column);
            if (cell == null || cell.IsEmpty())
            {
                return false;
            }

            hasRawValue = true;
            if (cell.TryGetValue(out double numeric))
            {
                value = numeric;
                return true;
            }

            var text = cell.GetString();
            if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed) ||
                double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private static bool TryGetBooleanValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key, out bool value, out bool hasRawValue)
        {
            value = false;
            hasRawValue = false;
            if (!headerMap.TryGetValue(key, out var column))
            {
                return false;
            }

            var cell = row.Cell(column);
            if (cell == null || cell.IsEmpty())
            {
                return false;
            }

            hasRawValue = true;
            if (cell.DataType == XLDataType.Boolean)
            {
                value = cell.GetBoolean();
                return true;
            }

            var text = cell.GetString().Trim();
            if (bool.TryParse(text, out var parsed))
            {
                value = parsed;
                return true;
            }

            if (int.TryParse(text, out var numeric))
            {
                value = numeric != 0;
                return true;
            }

            if (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static bool TryGetDateValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key, out DateTime value, out bool hasRawValue)
        {
            value = default;
            hasRawValue = false;
            if (!headerMap.TryGetValue(key, out var column))
            {
                return false;
            }

            var cell = row.Cell(column);
            if (cell == null || cell.IsEmpty())
            {
                return false;
            }

            hasRawValue = true;
            if (cell.TryGetValue(out DateTime parsedDate))
            {
                value = DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified);
                return true;
            }

            var text = cell.GetString();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate) ||
                DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate))
            {
                value = DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified);
                return true;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
            {
                try
                {
                    value = DateTime.SpecifyKind(DateTime.FromOADate(numeric), DateTimeKind.Unspecified);
                    return true;
                }
                catch
                {
                    // ignore conversion errors
                }
            }

            return false;
        }

        private MeasurementImportRowPreview ValidateImportRow(
            MeasurementImportRowInput input,
            IReadOnlyDictionary<int, string> emissionSources,
            IReadOnlyDictionary<string, ParameterLookup> parameters)
        {
            var preview = new MeasurementImportRowPreview
            {
                RowNumber = input.RowNumber,
                EmissionSourceId = input.EmissionSourceId,
                ParameterCode = input.ParameterCode,
                MeasurementDate = input.MeasurementDate,
                EntryDate = input.EntryDate,
                Value = input.Value,
                Unit = string.IsNullOrWhiteSpace(input.Unit) ? null : input.Unit.Trim(),
                Remark = string.IsNullOrWhiteSpace(input.Remark) ? null : input.Remark.Trim(),
                IsApproved = input.IsApproved ?? false,
                ApprovedAt = input.ApprovedAt
            };

            foreach (var parseError in input.Errors)
            {
                preview.Errors.Add(parseError);
            }

            if (!preview.EmissionSourceId.HasValue)
            {
                preview.Errors.Add("Emission Source ID is required.");
            }
            else if (!emissionSources.TryGetValue(preview.EmissionSourceId.Value, out var sourceName))
            {
                preview.Errors.Add($"Emission source #{preview.EmissionSourceId.Value} was not found.");
            }
            else
            {
                preview.EmissionSourceName = sourceName;
            }

            if (string.IsNullOrWhiteSpace(preview.ParameterCode))
            {
                preview.Errors.Add("Parameter code is required.");
            }
            else
            {
                var normalizedCode = preview.ParameterCode.Trim().ToUpperInvariant();
                preview.ParameterCode = normalizedCode;
                if (!parameters.TryGetValue(normalizedCode, out var parameter))
                {
                    preview.Errors.Add($"Parameter {normalizedCode} was not found.");
                }
                else
                {
                    preview.ParameterName = parameter.Label;
                    preview.ParameterType = parameter.Type;
                    if (string.IsNullOrWhiteSpace(preview.Unit))
                    {
                        preview.Unit = parameter.Unit;
                    }
                }
            }

            var effectiveMeasurementDate = preview.MeasurementDate ?? preview.EntryDate;
            if (!effectiveMeasurementDate.HasValue)
            {
                preview.Errors.Add("Measurement date is required.");
            }
            else
            {
                preview.MeasurementDate = effectiveMeasurementDate.Value;
            }

            if (!preview.EntryDate.HasValue)
            {
                preview.EntryDate = preview.MeasurementDate ?? DateTime.UtcNow;
            }

            if (!preview.Value.HasValue)
            {
                preview.Errors.Add("Value is required.");
            }

            if (preview.IsApproved && !preview.ApprovedAt.HasValue)
            {
                preview.ApprovedAt = DateTime.UtcNow;
            }

            return preview;
        }

        private sealed class MeasurementImportRowInput
        {
            public int RowNumber { get; set; }
            public int? EmissionSourceId { get; set; }
            public string? ParameterCode { get; set; }
            public DateTime? MeasurementDate { get; set; }
            public DateTime? EntryDate { get; set; }
            public double? Value { get; set; }
            public string? Unit { get; set; }
            public string? Remark { get; set; }
            public bool? IsApproved { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public IList<string> Errors { get; } = new List<string>();
        }

        private sealed class ParameterLookup
        {
            public string Code { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public string Type { get; set; } = "water";
        }
    }
}
