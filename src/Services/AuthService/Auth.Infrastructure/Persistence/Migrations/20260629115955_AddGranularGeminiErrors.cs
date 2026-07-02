using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGranularGeminiErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthenticationErrors",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConfigurationErrors",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvalidResponses",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServerErrors",
                table: "GeminiUsages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticationErrors",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "ConfigurationErrors",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "InvalidResponses",
                table: "GeminiUsages");

            migrationBuilder.DropColumn(
                name: "ServerErrors",
                table: "GeminiUsages");
        }
    }
}
