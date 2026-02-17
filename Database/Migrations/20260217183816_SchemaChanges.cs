using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestLog.Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class SchemaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CharacterClasses_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CharacterName",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "CharacterClasses",
                schema: "identity",
                newName: "CharacterClasses",
                newSchema: "public");

            migrationBuilder.CreateTable(
                name: "Adventurers",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CharacterClassId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adventurers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Adventurers_CharacterClasses_CharacterClassId",
                        column: x => x.CharacterClassId,
                        principalSchema: "public",
                        principalTable: "CharacterClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adventurers_CharacterClassId",
                schema: "public",
                table: "Adventurers",
                column: "CharacterClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adventurers",
                schema: "public");

            migrationBuilder.RenameTable(
                name: "CharacterClasses",
                schema: "public",
                newName: "CharacterClasses",
                newSchema: "identity");

            migrationBuilder.AddColumn<int>(
                name: "CharacterClassId",
                schema: "identity",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                schema: "identity",
                table: "AspNetUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers",
                column: "CharacterClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CharacterClasses_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers",
                column: "CharacterClassId",
                principalSchema: "identity",
                principalTable: "CharacterClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
