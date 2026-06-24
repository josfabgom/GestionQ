using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedsLabelPrint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsLabelPrint",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsLabelPrint",
                table: "Products");
        }
    }
}
