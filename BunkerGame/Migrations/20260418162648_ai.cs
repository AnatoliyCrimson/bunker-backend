using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class ai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AchievementVerdict",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BunkerDescription",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BunkerRoomsJson",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisasterDescription",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiPrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPrompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameEventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEventLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEventLogs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPrompts_Code",
                table: "AiPrompts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameEventLogs_GameId",
                table: "GameEventLogs",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiPrompts");

            migrationBuilder.DropTable(
                name: "GameEventLogs");

            migrationBuilder.DropColumn(
                name: "AchievementVerdict",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BunkerDescription",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BunkerRoomsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "DisasterDescription",
                table: "Games");
        }
    }
}
