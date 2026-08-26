using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTrackingToPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "PointsOfSale",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosIdentifier",
                table: "PointsOfSale",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncCustomers",
                table: "PointsOfSale",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SyncIpAddress",
                table: "PointsOfSale",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncOnlyWithStock",
                table: "PointsOfSale",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "PointsOfSale");

            migrationBuilder.DropColumn(
                name: "PosIdentifier",
                table: "PointsOfSale");

            migrationBuilder.DropColumn(
                name: "SyncCustomers",
                table: "PointsOfSale");

            migrationBuilder.DropColumn(
                name: "SyncIpAddress",
                table: "PointsOfSale");

            migrationBuilder.DropColumn(
                name: "SyncOnlyWithStock",
                table: "PointsOfSale");
        }
    }
}
