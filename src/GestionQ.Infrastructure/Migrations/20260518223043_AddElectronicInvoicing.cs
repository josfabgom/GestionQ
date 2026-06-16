using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddElectronicInvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectronicInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    PointOfSaleId = table.Column<int>(type: "int", nullable: false),
                    PointOfSaleNumber = table.Column<int>(type: "int", nullable: false),
                    InvoiceTypeCode = table.Column<int>(type: "int", nullable: false),
                    InvoiceTypeDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceNumber = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConceptCode = table.Column<int>(type: "int", nullable: false),
                    DocTypeCode = table.Column<int>(type: "int", nullable: false),
                    DocNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerTaxCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExemptAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CAE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CAEExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanMisMonExt = table.Column<bool>(type: "bit", nullable: false),
                    CondicionIVAReceptorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectronicInvoices_PointsOfSale_PointOfSaleId",
                        column: x => x.PointOfSaleId,
                        principalTable: "PointsOfSale",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectronicInvoices_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_PointOfSaleId",
                table: "ElectronicInvoices",
                column: "PointOfSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_SaleId",
                table: "ElectronicInvoices",
                column: "SaleId",
                unique: true,
                filter: "[SaleId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicInvoices");
        }
    }
}
