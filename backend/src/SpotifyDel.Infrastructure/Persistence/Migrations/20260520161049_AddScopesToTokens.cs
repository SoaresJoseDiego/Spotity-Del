using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyDel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScopesToTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "spotify_tokens",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "spotify_tokens");
        }
    }
}
