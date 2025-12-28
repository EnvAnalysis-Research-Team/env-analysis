using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace env_analysis_project.Models
{
    public class MeasurementResult
    {
        [Key]
        public int ResultID { get; set; }

        [ForeignKey("EmissionSource")]
        public int EmissionSourceID { get; set; }
        public EmissionSource EmissionSource { get; set; }

        [ForeignKey("Parameter")]
        [StringLength(50)]
        public string ParameterCode { get; set; }
        public Parameter Parameter { get; set; }

        public DateTime MeasurementDate { get; set; }

        public double? Value { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        public DateTime EntryDate { get; set; }

        [StringLength(255)]
        public string Remark { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
