using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueReportPerUserAndMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoomName",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_MessageId_ReportedByUserId",
                table: "Reports",
                columns: new[] { "MessageId", "ReportedByUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_MessageId_ReportedByUserId",
                table: "Reports");

            migrationBuilder.AddColumn<string>(
                name: "RoomName",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
