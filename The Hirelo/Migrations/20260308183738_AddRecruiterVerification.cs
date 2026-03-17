using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JdFileUrl",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptUrl",
                table: "InterviewSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "InterviewSessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecruiterVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecruiterProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    CompanyTaxCode = table.Column<string>(type: "text", nullable: false),
                    RecruiterEmail = table.Column<string>(type: "text", nullable: true),
                    ImagesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruiterVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruiterVerifications_RecruiterProfiles_RecruiterProfileId",
                        column: x => x.RecruiterProfileId,
                        principalTable: "RecruiterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterVerifications_RecruiterProfileId",
                table: "RecruiterVerifications",
                column: "RecruiterProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecruiterVerifications");

            migrationBuilder.DropColumn(
                name: "JdFileUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TranscriptUrl",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "InterviewSessions");
        }
    }
}
