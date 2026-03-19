using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class acd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CandidateProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileKey",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gaps",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "CandidateProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MatchingScore",
                table: "CandidateProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedSkillsJson",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CandidateProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Strengths",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CandidateProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_JobId",
                table: "CandidateProfiles",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_Jobs_JobId",
                table: "CandidateProfiles",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateProfiles_Jobs_JobId",
                table: "CandidateProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_JobId",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "FileKey",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Gaps",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "MatchingScore",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ParsedSkillsJson",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Strengths",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CandidateProfiles");
        }
    }
}
