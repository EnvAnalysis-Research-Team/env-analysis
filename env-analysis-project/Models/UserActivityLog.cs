using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace env_analysis_project.Models
{
    [Index(nameof(OccurredAt))]
    public class UserActivityLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityName { get; set; }

        [StringLength(100)]
        public string? EntityId { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public string? MetadataJson { get; set; }
    }
}
