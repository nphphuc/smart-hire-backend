using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVerifiedToRecruiterProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "RecruiterProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "RecruiterProfiles");
        }
    }
}
