using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace env_analysis_project.Migrations
{
    /// <inheritdoc />
    public partial class MoveMeasurementTypeToParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "MeasurementResult");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Parameter",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "water");

            migrationBuilder.Sql("UPDATE [Parameter] SET [Type] = 'water' WHERE [Type] IS NULL OR LTRIM(RTRIM([Type])) = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Parameter");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "MeasurementResult",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "water");
        }
    }
}
