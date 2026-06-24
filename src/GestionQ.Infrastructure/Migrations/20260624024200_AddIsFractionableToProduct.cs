using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFractionableToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFractionable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Products SET IsFractionable = 1 WHERE IsPesable = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFractionable",
                table: "Products");
        }
    }
}
