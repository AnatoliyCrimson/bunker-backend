using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class awdawdkk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisasterName",
                table: "Games",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisasterName",
                table: "Games");
        }
    }
}
