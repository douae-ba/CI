using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddModifieManuellementFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateModificationManuelle",
                table: "Suggestions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModifieManuellement",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateModificationManuelle",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "ModifieManuellement",
                table: "Suggestions");
        }
    }
}
