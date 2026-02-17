using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuestLog.Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterClassId",
                schema: "identity",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CharacterClasses",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterClasses", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CharacterClasses_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CharacterClasses",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CharacterClassId",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
