using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuggestionAndExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SuggestionNumber",
                table: "Suggestions",
                newName: "NumeroSuggestion");

            migrationBuilder.AlterColumn<int>(
                name: "Mois",
                table: "Suggestions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateSuggestion",
                table: "Suggestions",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateTable(
                name: "Exports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceFileUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CibleFileUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DateExport = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NbSugg = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions",
                column: "NumeroSuggestion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scopes_Nom",
                table: "Scopes",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departements_Nom",
                table: "Departements",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exports");

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions");

            migrationBuilder.DropIndex(
                name: "IX_Scopes_Nom",
                table: "Scopes");

            migrationBuilder.DropIndex(
                name: "IX_Departements_Nom",
                table: "Departements");

            migrationBuilder.RenameColumn(
                name: "NumeroSuggestion",
                table: "Suggestions",
                newName: "SuggestionNumber");

            migrationBuilder.AlterColumn<int>(
                name: "Mois",
                table: "Suggestions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateSuggestion",
                table: "Suggestions",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);
        }
    }
}
