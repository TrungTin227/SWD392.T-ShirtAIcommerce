using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerInfoAndPhaseToCustomDesignPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerAddress",
                table: "CustomDesignPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerName",
                table: "CustomDesignPayments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerPhone",
                table: "CustomDesignPayments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayerAddress",
                table: "CustomDesignPayments");

            migrationBuilder.DropColumn(
                name: "PayerName",
                table: "CustomDesignPayments");

            migrationBuilder.DropColumn(
                name: "PayerPhone",
                table: "CustomDesignPayments");
        }
    }
}
