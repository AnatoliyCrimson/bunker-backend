using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class awdawdkkawdawd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                table: "GameEventLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "GameEventLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
