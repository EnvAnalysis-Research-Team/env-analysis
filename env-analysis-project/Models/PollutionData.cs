using Microsoft.ML.Data;
using System;
using System.Collections.Generic;

namespace env_analysis_project.Models
{
    public class PollutionData
    {
        // CSV header: Type,Source,Parameter,Value,Unit,Measurement Date,Status,Approved At,Remark
        [LoadColumn(0)] public string Type { get; set; } = string.Empty;
        [LoadColumn(1)] public string Source { get; set; } = string.Empty;
        [LoadColumn(2)] public string Parameter { get; set; } = string.Empty;
        [LoadColumn(3)] public float Value { get; set; }
        [LoadColumn(4)] public string Unit { get; set; } = string.Empty;
        [LoadColumn(5)] public string MeasurementDate { get; set; } = string.Empty;
        [LoadColumn(6)] public string Status { get; set; } = string.Empty;
        [LoadColumn(7)] public string ApprovedAt { get; set; } = string.Empty;
        [LoadColumn(8)] public string Remark { get; set; } = string.Empty;

        // NoColumn so ML.NET doesn't expect a LoadColumn for TimeIndex
    }

    public class PollutionPrediction
    {
        [ColumnName("Score")] public float PredictedValue { get; set; }
    }

    public class SpikePredictionRow
    {
        [VectorType(3)] public double[] SpikePrediction { get; set; } = Array.Empty<double>();
    }

    public class PredictionRow
    {
        public string ParameterDisplayName { get; set; } = string.Empty;
        public DateTime MeasurementDate { get; set; }
        public float ActualValue { get; set; }
        public float PredictedValue { get; set; }
        public bool IsWarning { get; set; }
        public bool IsSpike { get; set; }
        public float? Threshold { get; set; }
    }

    public class FutureForecast
    {
        public DateTime Date { get; set; }
        public float Value { get; set; }
    }

    public class PredictionResult
    {
        public string YearMonth { get; set; }
        public double RMSE { get; set; }
        public double R2 { get; set; }
       
        public List<PredictionRow> Rows { get; set; } = new List<PredictionRow>();
        public Dictionary<string, List<FutureForecast>> FutureForecasts { get; set; } = new Dictionary<string, List<FutureForecast>>();
        public int WarningCount { get; set; }
        public int SpikeCount { get; set; }
    }
}