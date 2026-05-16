using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsOfSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointOfSaleId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointOfSaleId",
                table: "CashRegisters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PointsOfSale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsOfSale", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PointOfSaleId",
                table: "Sales",
                column: "PointOfSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_PointOfSaleId",
                table: "CashRegisters",
                column: "PointOfSaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisters_PointsOfSale_PointOfSaleId",
                table: "CashRegisters",
                column: "PointOfSaleId",
                principalTable: "PointsOfSale",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_PointsOfSale_PointOfSaleId",
                table: "Sales",
                column: "PointOfSaleId",
                principalTable: "PointsOfSale",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisters_PointsOfSale_PointOfSaleId",
                table: "CashRegisters");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_PointsOfSale_PointOfSaleId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "PointsOfSale");

            migrationBuilder.DropIndex(
                name: "IX_Sales_PointOfSaleId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_PointOfSaleId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "PointOfSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PointOfSaleId",
                table: "CashRegisters");
        }
    }
}
