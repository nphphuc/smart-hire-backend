using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecruiterVerificationV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RecruiterVerifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerificationId",
                table: "RecruiterProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterProfiles_VerificationId",
                table: "RecruiterProfiles",
                column: "VerificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecruiterProfiles_RecruiterVerifications_VerificationId",
                table: "RecruiterProfiles",
                column: "VerificationId",
                principalTable: "RecruiterVerifications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruiterProfiles_RecruiterVerifications_VerificationId",
                table: "RecruiterProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RecruiterProfiles_VerificationId",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationId",
                table: "RecruiterProfiles");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Companies");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RecruiterVerifications",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
