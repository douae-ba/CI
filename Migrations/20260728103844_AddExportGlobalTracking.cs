using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddExportGlobalTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExportGlobal",
                table: "Suggestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExporteVersGlobal",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateExportGlobal",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "ExporteVersGlobal",
                table: "Suggestions");
        }
    }
}
