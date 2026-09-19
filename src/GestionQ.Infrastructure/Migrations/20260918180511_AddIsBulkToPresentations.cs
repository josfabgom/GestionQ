using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBulkToPresentations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBulk",
                table: "ProductPresentations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBulk",
                table: "ProductPresentations");
        }
    }
}
