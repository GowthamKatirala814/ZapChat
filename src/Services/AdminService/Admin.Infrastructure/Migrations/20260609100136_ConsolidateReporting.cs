using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportedMessages");

            migrationBuilder.RenameColumn(
                name: "TargetType",
                table: "AuditLogs",
                newName: "EntityType");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "AuditLogs",
                newName: "EntityId");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoRemoved",
                table: "Reports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoRemoved",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "AuditLogs",
                newName: "TargetType");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "AuditLogs",
                newName: "TargetId");

            migrationBuilder.CreateTable(
                name: "ReportedMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAutoRemoved = table.Column<bool>(type: "bit", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportedMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_MessageId",
                table: "ReportedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_ReportedAt",
                table: "ReportedMessages",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedMessages_Status",
                table: "ReportedMessages",
                column: "Status");
        }
    }
}
