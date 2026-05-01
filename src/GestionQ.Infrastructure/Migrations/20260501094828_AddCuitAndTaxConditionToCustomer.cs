using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCuitAndTaxConditionToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cuit",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxConditionId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxConditionId",
                table: "Customers",
                column: "TaxConditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_TaxConditions_TaxConditionId",
                table: "Customers",
                column: "TaxConditionId",
                principalTable: "TaxConditions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_TaxConditions_TaxConditionId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxConditionId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Cuit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxConditionId",
                table: "Customers");
        }
    }
}
