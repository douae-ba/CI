using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CI.Migrations
{
    /// <inheritdoc />
    public partial class MakeNumeroSuggestionNonUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions",
                column: "NumeroSuggestion");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_SourceRowNo",
                table: "Suggestions",
                column: "SourceRowNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions");

            migrationBuilder.DropIndex(
                name: "IX_Suggestions_SourceRowNo",
                table: "Suggestions");

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_NumeroSuggestion",
                table: "Suggestions",
                column: "NumeroSuggestion",
                unique: true);
        }
    }
}
