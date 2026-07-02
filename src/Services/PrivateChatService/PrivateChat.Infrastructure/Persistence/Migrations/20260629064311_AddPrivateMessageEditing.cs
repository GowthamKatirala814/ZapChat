using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateMessageEditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PrivateModerationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnonymousName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PrivateModerationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateModerationAuditLogs_Category",
                table: "PrivateModerationAuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateModerationAuditLogs_ConversationId",
                table: "PrivateModerationAuditLogs",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateModerationAuditLogs_Timestamp",
                table: "PrivateModerationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateModerationAuditLogs_UserId",
                table: "PrivateModerationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateModerationAuditLogs_WasAllowed",
                table: "PrivateModerationAuditLogs",
                column: "WasAllowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateModerationAuditLogs");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "Messages");
        }
    }
}
