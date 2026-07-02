using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLastMessageAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessagePreview",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User1LastReadAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User2LastReadAt",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                column: "LastMessageAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations");

            migrationBuilder.AddColumn<string>(
                name: "LastMessagePreview",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "User1LastReadAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "User2LastReadAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);
        }
    }
}
