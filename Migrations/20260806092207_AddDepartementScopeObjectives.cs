using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartementScopeObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartementScopeObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartementId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScopeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Mois = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartementScopeObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartementScopeObjectives_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartementScopeObjectives_Scopes_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartementScopeObjectives_DepartementId_ScopeId_Year_Mois",
                table: "DepartementScopeObjectives",
                columns: new[] { "DepartementId", "ScopeId", "Year", "Mois" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartementScopeObjectives_ScopeId",
                table: "DepartementScopeObjectives",
                column: "ScopeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartementScopeObjectives");
        }
    }
}
