using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosTicketNumberToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PosTicketNumber",
                table: "Sales",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosTicketNumber",
                table: "Sales");
        }
    }
}
