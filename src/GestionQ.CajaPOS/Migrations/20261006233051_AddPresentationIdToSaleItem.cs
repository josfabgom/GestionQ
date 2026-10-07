using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.CajaPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationIdToSaleItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PresentationId",
                table: "SaleItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBulk",
                table: "ProductPresentations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_PresentationId",
                table: "SaleItems",
                column: "PresentationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_ProductPresentations_PresentationId",
                table: "SaleItems",
                column: "PresentationId",
                principalTable: "ProductPresentations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_ProductPresentations_PresentationId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_PresentationId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PresentationId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "IsBulk",
                table: "ProductPresentations");
        }
    }
}
