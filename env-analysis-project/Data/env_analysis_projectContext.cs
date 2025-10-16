using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using env_analysis_project.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace env_analysis_project.Data
{
    public class env_analysis_projectContext : IdentityDbContext<ApplicationUser>
    {
        public env_analysis_projectContext (DbContextOptions<env_analysis_projectContext> options)
            : base(options)
        {
        }

        public DbSet<env_analysis_project.Models.EmissionSource> EmissionSource { get; set; } = default!;
        public DbSet<env_analysis_project.Models.MeasurementResult> MeasurementResult { get; set; } = default!;
        public DbSet<env_analysis_project.Models.Parameter> Parameter { get; set; } = default!;
        public DbSet<env_analysis_project.Models.SourceType> SourceType { get; set; } = default!;
    }
}
