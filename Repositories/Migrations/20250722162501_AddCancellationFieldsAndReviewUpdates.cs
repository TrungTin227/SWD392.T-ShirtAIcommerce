using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationFieldsAndReviewUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "CancellationImageUrls",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationStatus",
                table: "Orders",
                type: "nvarchar(max)", 
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ProductVariants_ProductVariantId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductVariantId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "Reviews");

            // Drop các trường của Order
            migrationBuilder.DropColumn(
                name: "CancellationImageUrls",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "Orders");
        }
    }
}