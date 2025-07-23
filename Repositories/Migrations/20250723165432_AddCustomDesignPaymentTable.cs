using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    public partial class AddCustomDesignPaymentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomDesignPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomDesignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", nullable: false), // Đổi thành string
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false), // Đổi thành string
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true) // Thêm trường PaidAt
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDesignPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDesignPayments_CustomDesigns_CustomDesignId",
                        column: x => x.CustomDesignId,
                        principalTable: "CustomDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesignPayments_CustomDesignId",
                table: "CustomDesignPayments",
                column: "CustomDesignId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDesignPayments");
        }
    }
}
