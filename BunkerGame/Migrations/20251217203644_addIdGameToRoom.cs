using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class addIdGameToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GameId",
                table: "Rooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsGameStart",
                table: "Rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "Games",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsGameStart",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Games");
        }
    }
}
