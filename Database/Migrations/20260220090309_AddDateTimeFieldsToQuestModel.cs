using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestLog.Backend.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDateTimeFieldsToQuestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "public",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "public",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "public",
                table: "Quests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "public",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "public",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "public",
                table: "Quests");
        }
    }
}
