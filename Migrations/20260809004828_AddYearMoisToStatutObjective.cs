using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddYearMoisToStatutObjective : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatutObjectives_Statut",
                table: "StatutObjectives");

            migrationBuilder.AddColumn<int>(
                name: "Mois",
                table: "StatutObjectives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "StatutObjectives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StatutObjectives_Statut_Year_Mois",
                table: "StatutObjectives",
                columns: new[] { "Statut", "Year", "Mois" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatutObjectives_Statut_Year_Mois",
                table: "StatutObjectives");

            migrationBuilder.DropColumn(
                name: "Mois",
                table: "StatutObjectives");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "StatutObjectives");

            migrationBuilder.CreateIndex(
                name: "IX_StatutObjectives_Statut",
                table: "StatutObjectives",
                column: "Statut",
                unique: true);
        }
    }
}
