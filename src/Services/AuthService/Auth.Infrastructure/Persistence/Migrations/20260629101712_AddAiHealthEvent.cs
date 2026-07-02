using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiHealthEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockedMessages",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatus",
                table: "GeminiUsages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Error429s",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FailedRequests",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                table: "GeminiUsages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedModeration",
                table: "GeminiUsages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSuccessfulModeration",
                table: "GeminiUsages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryTime",
                table: "GeminiUsages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafeMessages",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulRequests",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutErrors",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedMessages",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "CurrentStatus",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "Error429s",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "FailedRequests",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "LastFailedModeration",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulModeration",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "RecoveryTime",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "SafeMessages",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "SuccessfulRequests",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "TimeoutErrors",
                table: "GeminiUsages");
        }
    }
}
