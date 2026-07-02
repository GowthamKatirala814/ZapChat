using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrivateChatMutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "User1Muted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User1MutedUntil",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User2Muted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User2MutedUntil",
                table: "Conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "User1Muted",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "User1MutedUntil",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "User2Muted",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "User2MutedUntil",
                table: "Conversations",
                type: "datetime2",
                nullable: true);
        }
    }
}
