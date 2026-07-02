using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDenormalizedPerfColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastMessagePreview",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User1UnreadCount",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "User2UnreadCount",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessagePreview",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User1UnreadCount",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "User2UnreadCount",
                table: "Conversations");
        }
    }
}
