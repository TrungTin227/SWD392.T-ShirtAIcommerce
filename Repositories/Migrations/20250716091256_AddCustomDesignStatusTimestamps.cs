using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDesignStatusTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoneAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderCreatedAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippingStartedAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "DoneAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "OrderCreatedAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ShippingStartedAt",
                table: "CustomDesigns");
        }
    }
}
