using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoomStates");

            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "LastMessagePreview",
                table: "ChatRooms");

            migrationBuilder.CreateTable(
                name: "ModerationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnonymousName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageSnippet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    WasAllowed = table.Column<bool>(type: "bit", nullable: false),
                    WasRuleBasedBlock = table.Column<bool>(type: "bit", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditLogs_Category",
                table: "ModerationAuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditLogs_RoomId",
                table: "ModerationAuditLogs",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditLogs_Timestamp",
                table: "ModerationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditLogs_UserId",
                table: "ModerationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationAuditLogs_WasAllowed",
                table: "ModerationAuditLogs",
                column: "WasAllowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationAuditLogs");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                table: "ChatRooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessagePreview",
                table: "ChatRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserRoomStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoomStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoomStates_ChatRooms_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoomStates_ChatRoomId",
                table: "UserRoomStates",
                column: "ChatRoomId");
        }
    }
}
