using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatutTargetToCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetPercent",
                table: "StatutObjectives",
                newName: "TargetCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetCount",
                table: "StatutObjectives",
                newName: "TargetPercent");
        }
    }
}
