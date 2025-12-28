using System;
using System.Collections.Generic;

namespace env_analysis_project.Models
{
    public sealed class MeasurementImportConfirmRequest
    {
        public IList<MeasurementImportRowRequest> Rows { get; init; } = new List<MeasurementImportRowRequest>();
    }

    public sealed class MeasurementImportRowRequest
    {
        public int RowNumber { get; set; }
        public int? EmissionSourceId { get; set; }
        public string? ParameterCode { get; set; }
        public DateTime? MeasurementDate { get; set; }
        public DateTime? EntryDate { get; set; }
        public double? Value { get; set; }
        public string? Unit { get; set; }
        public string? Remark { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public sealed class MeasurementImportPreviewResponse
    {
        public IReadOnlyList<MeasurementImportRowPreview> Rows { get; init; } = Array.Empty<MeasurementImportRowPreview>();
        public int TotalRows { get; init; }
        public int ValidRows { get; init; }
        public int InvalidRows { get; init; }
    }

    public sealed class MeasurementImportConfirmResponse
    {
        public int TotalRows { get; init; }
        public int InsertedRows { get; init; }
        public int FailedRows { get; init; }
        public IReadOnlyList<MeasurementImportRowPreview> Rows { get; init; } = Array.Empty<MeasurementImportRowPreview>();
    }

    public sealed class MeasurementImportRowPreview
    {
        public int RowNumber { get; set; }
        public int? EmissionSourceId { get; set; }
        public string? EmissionSourceName { get; set; }
        public string? ParameterCode { get; set; }
        public string? ParameterName { get; set; }
        public string? ParameterType { get; set; }
        public DateTime? MeasurementDate { get; set; }
        public DateTime? EntryDate { get; set; }
        public double? Value { get; set; }
        public string? Unit { get; set; }
        public string? Remark { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public IList<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
