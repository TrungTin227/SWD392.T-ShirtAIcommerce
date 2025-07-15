using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantToReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductVariantId",
                table: "Reviews",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ProductVariants_ProductVariantId",
                table: "Reviews",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
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
        }
    }
}
