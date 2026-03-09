using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_Hirelo.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecruiterVerificationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruiterVerifications_RecruiterProfiles_RecruiterProfileId",
                table: "RecruiterVerifications");

            migrationBuilder.DropIndex(
                name: "IX_RecruiterVerifications_RecruiterProfileId",
                table: "RecruiterVerifications");

            migrationBuilder.RenameColumn(
                name: "RecruiterProfileId",
                table: "RecruiterVerifications",
                newName: "UserId");

            migrationBuilder.AlterColumn<string>(
                name: "CognitoSub",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RecruiterVerifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CognitoSub",
                table: "Users",
                column: "CognitoSub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterVerifications_UserId",
                table: "RecruiterVerifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecruiterVerifications_Users_UserId",
                table: "RecruiterVerifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecruiterVerifications_Users_UserId",
                table: "RecruiterVerifications");

            migrationBuilder.DropIndex(
                name: "IX_Users_CognitoSub",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RecruiterVerifications_UserId",
                table: "RecruiterVerifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RecruiterVerifications");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "RecruiterVerifications",
                newName: "RecruiterProfileId");

            migrationBuilder.AlterColumn<string>(
                name: "CognitoSub",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterVerifications_RecruiterProfileId",
                table: "RecruiterVerifications",
                column: "RecruiterProfileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RecruiterVerifications_RecruiterProfiles_RecruiterProfileId",
                table: "RecruiterVerifications",
                column: "RecruiterProfileId",
                principalTable: "RecruiterProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
