using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestLog.Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClassProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Xp",
                schema: "public",
                table: "Adventurers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Xp",
                schema: "public",
                table: "Adventurers");
        }
    }
}
