using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestLog.Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class ClassProgressionSchemaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ClassProgressions_CharacterClassId",
                schema: "public",
                table: "ClassProgressions",
                column: "CharacterClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassProgressions_CharacterClasses_CharacterClassId",
                schema: "public",
                table: "ClassProgressions",
                column: "CharacterClassId",
                principalSchema: "public",
                principalTable: "CharacterClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassProgressions_CharacterClasses_CharacterClassId",
                schema: "public",
                table: "ClassProgressions");

            migrationBuilder.DropIndex(
                name: "IX_ClassProgressions_CharacterClassId",
                schema: "public",
                table: "ClassProgressions");
        }
    }
}
