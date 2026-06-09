using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poll.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPollAnalyticsAndReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Polls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Downvotes",
                table: "Polls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Upvotes",
                table: "Polls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PollReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsUpvote = table.Column<bool>(type: "bit", nullable: false),
                    ReactedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollReactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PollReactions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "Downvotes",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "Upvotes",
                table: "Polls");
        }
    }
}
