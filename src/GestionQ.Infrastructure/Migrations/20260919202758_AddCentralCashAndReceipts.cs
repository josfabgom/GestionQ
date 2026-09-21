using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCentralCashAndReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    PurchaseId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CheckBank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CentralCashMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SourceCashRegisterId = table.Column<int>(type: "int", nullable: true),
                    PaymentReceiptId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentralCashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentralCashMovements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CentralCashMovements_CashRegisters_SourceCashRegisterId",
                        column: x => x.SourceCashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CentralCashMovements_PaymentReceipts_PaymentReceiptId",
                        column: x => x.PaymentReceiptId,
                        principalTable: "PaymentReceipts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentralCashMovements_PaymentReceiptId",
                table: "CentralCashMovements",
                column: "PaymentReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_CentralCashMovements_SourceCashRegisterId",
                table: "CentralCashMovements",
                column: "SourceCashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_CentralCashMovements_UserId",
                table: "CentralCashMovements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_PaymentMethodId",
                table: "PaymentReceipts",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_PurchaseId",
                table: "PaymentReceipts",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_SupplierId",
                table: "PaymentReceipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_UserId",
                table: "PaymentReceipts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentralCashMovements");

            migrationBuilder.DropTable(
                name: "PaymentReceipts");
        }
    }
}
