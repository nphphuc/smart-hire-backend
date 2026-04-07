using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class AddJobExtendedMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMin",
                table: "Jobs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryMax",
                table: "Jobs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "Jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Location", table: "Jobs");
            migrationBuilder.DropColumn(name: "EmploymentType", table: "Jobs");
            migrationBuilder.DropColumn(name: "SalaryMin", table: "Jobs");
            migrationBuilder.DropColumn(name: "SalaryMax", table: "Jobs");
            migrationBuilder.DropColumn(name: "ExperienceLevel", table: "Jobs");
        }
    }
}
