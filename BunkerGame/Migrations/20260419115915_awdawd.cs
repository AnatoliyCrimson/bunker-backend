using System;
using System.Collections.Generic;
using BunkerGame.Models.GameModels;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class awdawd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BunkerRoomsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "GameEventLogs");

            migrationBuilder.AddColumn<List<BunkerRoom>>(
                name: "BunkerRooms",
                table: "Games",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BunkerRooms",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "BunkerRoomsJson",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "GameEventLogs",
                type: "uuid",
                nullable: true);
        }
    }
}
