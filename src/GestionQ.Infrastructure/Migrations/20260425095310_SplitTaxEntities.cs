using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitTaxEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_VatConditions_VatConditionId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "VatConditions");

            migrationBuilder.RenameColumn(
                name: "VatConditionId",
                table: "Suppliers",
                newName: "TaxConditionId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_VatConditionId",
                table: "Suppliers",
                newName: "IX_Suppliers_TaxConditionId");

            migrationBuilder.AddColumn<int>(
                name: "VatRateId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_VatRateId",
                table: "Products",
                column: "VatRateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_VatRates_VatRateId",
                table: "Products",
                column: "VatRateId",
                principalTable: "VatRates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_TaxConditions_TaxConditionId",
                table: "Suppliers",
                column: "TaxConditionId",
                principalTable: "TaxConditions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_VatRates_VatRateId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_TaxConditions_TaxConditionId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "TaxConditions");

            migrationBuilder.DropTable(
                name: "VatRates");

            migrationBuilder.DropIndex(
                name: "IX_Products_VatRateId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VatRateId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "TaxConditionId",
                table: "Suppliers",
                newName: "VatConditionId");

            migrationBuilder.RenameIndex(
                name: "IX_Suppliers_TaxConditionId",
                table: "Suppliers",
                newName: "IX_Suppliers_VatConditionId");

            migrationBuilder.CreateTable(
                name: "VatConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatConditions", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_VatConditions_VatConditionId",
                table: "Suppliers",
                column: "VatConditionId",
                principalTable: "VatConditions",
                principalColumn: "Id");
        }
    }
}
