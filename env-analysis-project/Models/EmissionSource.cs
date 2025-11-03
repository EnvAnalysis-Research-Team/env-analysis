using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace env_analysis_project.Models
{
    public class EmissionSource
    {
        [Key]
        public int EmissionSourceID { get; set; }

        [Required, StringLength(50)]
        public string SourceCode { get; set; }

        [Required, StringLength(100)]
        public string SourceName { get; set; }

        [ForeignKey("SourceType")]
        public int SourceTypeID { get; set; }

        public SourceType? SourceType { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<MeasurementResult>? MeasurementResults { get; set; }
    }
}
