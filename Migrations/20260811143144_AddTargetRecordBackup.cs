using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetRecordBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TargetRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Plant = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestionNumber = table.Column<string>(type: "TEXT", nullable: false),
                    RegisterDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RegisterMonth = table.Column<string>(type: "TEXT", nullable: false),
                    ImprovementType = table.Column<string>(type: "TEXT", nullable: false),
                    Department = table.Column<string>(type: "TEXT", nullable: false),
                    AttackedLosses = table.Column<string>(type: "TEXT", nullable: false),
                    DelayStatus = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ClosureStatus = table.Column<string>(type: "TEXT", nullable: false),
                    TotalSaving = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetRecords_SourceFilePath_SuggestionNumber",
                table: "TargetRecords",
                columns: new[] { "SourceFilePath", "SuggestionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TargetRecords");
        }
    }
}
