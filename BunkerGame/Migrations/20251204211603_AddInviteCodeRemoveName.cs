using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BunkerGame.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodeRemoveName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Rooms",
                newName: "InviteCode");

            migrationBuilder.RenameColumn(
                name: "Phobia",
                table: "Player",
                newName: "SpecialSkill");

            migrationBuilder.RenameColumn(
                name: "IsReady",
                table: "Player",
                newName: "IsKicked");

            migrationBuilder.RenameColumn(
                name: "Health",
                table: "Player",
                newName: "Psychology");

            migrationBuilder.RenameColumn(
                name: "Age",
                table: "Player",
                newName: "Physiology");

            migrationBuilder.AddColumn<string>(
                name: "Inventory",
                table: "Player",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "RevealedTraitKeys",
                table: "Player",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inventory",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "RevealedTraitKeys",
                table: "Player");

            migrationBuilder.RenameColumn(
                name: "InviteCode",
                table: "Rooms",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SpecialSkill",
                table: "Player",
                newName: "Phobia");

            migrationBuilder.RenameColumn(
                name: "Psychology",
                table: "Player",
                newName: "Health");

            migrationBuilder.RenameColumn(
                name: "Physiology",
                table: "Player",
                newName: "Age");

            migrationBuilder.RenameColumn(
                name: "IsKicked",
                table: "Player",
                newName: "IsReady");
        }
    }
}
