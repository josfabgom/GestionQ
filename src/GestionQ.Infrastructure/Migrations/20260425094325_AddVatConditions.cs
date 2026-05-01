using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVatConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatConditionId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VatConditions",
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
                    table.PrimaryKey("PK_VatConditions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_VatConditionId",
                table: "Suppliers",
                column: "VatConditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_VatConditions_VatConditionId",
                table: "Suppliers",
                column: "VatConditionId",
                principalTable: "VatConditions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_VatConditions_VatConditionId",
                table: "Suppliers");

            migrationBuilder.DropTable(
                name: "VatConditions");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_VatConditionId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "VatConditionId",
                table: "Suppliers");
        }
    }
}
