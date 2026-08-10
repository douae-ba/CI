using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyResponsable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Plant = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestionNumber = table.Column<string>(type: "TEXT", nullable: false),
                    DateSuggestion = table.Column<DateTime>(type: "date", nullable: false),
                    ScopeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneLigne = table.Column<string>(type: "TEXT", nullable: false),
                    Situation = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DepartementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Responsable = table.Column<string>(type: "TEXT", nullable: false),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    DateMiseEnOeuvre = table.Column<DateTime>(type: "date", nullable: true),
                    Economies = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    KaizenNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Mois = table.Column<int>(type: "INTEGER", nullable: false),
                    Kaizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suggestions_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Suggestions_Scopes_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_DepartementId",
                table: "Suggestions",
                column: "DepartementId");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_ScopeId",
                table: "Suggestions",
                column: "ScopeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suggestions");

            migrationBuilder.DropTable(
                name: "Departements");

            migrationBuilder.DropTable(
                name: "Scopes");
        }
    }
}
