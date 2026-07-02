using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGeminiUsageTrackerToGeminiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeminiUsageTrackers");

            migrationBuilder.CreateTable(
                name: "GeminiUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestsToday = table.Column<int>(type: "int", nullable: false),
                    EstimatedDailyQuota = table.Column<int>(type: "int", nullable: false),
                    UsagePercentage = table.Column<double>(type: "float", nullable: false),
                    LastThresholdReached = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailSent50 = table.Column<bool>(type: "bit", nullable: false),
                    EmailSent90 = table.Column<bool>(type: "bit", nullable: false),
                    EmailSent100 = table.Column<bool>(type: "bit", nullable: false),
                    QuotaExhausted = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeminiUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeminiUsages_Date",
                table: "GeminiUsages",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeminiUsages");

            migrationBuilder.CreateTable(
                name: "GeminiUsageTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HasSent100Percent = table.Column<bool>(type: "bit", nullable: false),
                    HasSent50Percent = table.Column<bool>(type: "bit", nullable: false),
                    HasSent90Percent = table.Column<bool>(type: "bit", nullable: false),
                    TotalRequests = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeminiUsageTrackers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeminiUsageTrackers_Date",
                table: "GeminiUsageTrackers",
                column: "Date",
                unique: true);
        }
    }
}
