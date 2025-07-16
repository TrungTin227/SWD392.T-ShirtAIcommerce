using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptTextAndUserToCustomDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomDesigns_AspNetUsers_StaffId",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "EstimatedDays",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "LogoPosition",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "LogoText",
                table: "CustomDesigns");

            migrationBuilder.RenameColumn(
                name: "StaffNotes",
                table: "CustomDesigns",
                newName: "PromptText");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "CustomDesigns",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomDesigns_StaffId",
                table: "CustomDesigns",
                newName: "IX_CustomDesigns_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomDesigns_AspNetUsers_ApplicationUserId",
                table: "CustomDesigns",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomDesigns_AspNetUsers_ApplicationUserId",
                table: "CustomDesigns");

            migrationBuilder.RenameColumn(
                name: "PromptText",
                table: "CustomDesigns",
                newName: "StaffNotes");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "CustomDesigns",
                newName: "StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomDesigns_ApplicationUserId",
                table: "CustomDesigns",
                newName: "IX_CustomDesigns_StaffId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDays",
                table: "CustomDesigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LogoPosition",
                table: "CustomDesigns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoText",
                table: "CustomDesigns",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomDesigns_AspNetUsers_StaffId",
                table: "CustomDesigns",
                column: "StaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
