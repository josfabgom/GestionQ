using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoOAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "MercadoPagoConfigs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MpUserId",
                table: "MercadoPagoConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "MercadoPagoConfigs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "MercadoPagoConfigs");

            migrationBuilder.DropColumn(
                name: "MpUserId",
                table: "MercadoPagoConfigs");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "MercadoPagoConfigs");
        }
    }
}
