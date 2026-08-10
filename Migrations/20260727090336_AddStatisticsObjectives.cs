using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetPercent",
                table: "Scopes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Headcount",
                table: "Departements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DepartementAnnualObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartementAnnualObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartementAnnualObjectives_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartementMonthlyObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Mois = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartementMonthlyObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartementMonthlyObjectives_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatutObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutObjectives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartementAnnualObjectives_DepartementId_Year",
                table: "DepartementAnnualObjectives",
                columns: new[] { "DepartementId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartementMonthlyObjectives_DepartementId_Year_Mois",
                table: "DepartementMonthlyObjectives",
                columns: new[] { "DepartementId", "Year", "Mois" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatutObjectives_Statut",
                table: "StatutObjectives",
                column: "Statut",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartementAnnualObjectives");

            migrationBuilder.DropTable(
                name: "DepartementMonthlyObjectives");

            migrationBuilder.DropTable(
                name: "StatutObjectives");

            migrationBuilder.DropColumn(
                name: "TargetPercent",
                table: "Scopes");

            migrationBuilder.DropColumn(
                name: "Headcount",
                table: "Departements");
        }
    }
}
