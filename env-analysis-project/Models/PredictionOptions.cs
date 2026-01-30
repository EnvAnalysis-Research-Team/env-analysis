namespace env_analysis_project.Models
{
    public class PredictionOptions
    {
        public int MinSamplesForForecast { get; set; } = 5;
        public int ForecastHorizon { get; set; } = 5;
        public int DefaultStepMinutes { get; set; } = 60;
        public string ApprovedStatus { get; set; } = "Approved";
    }
}
