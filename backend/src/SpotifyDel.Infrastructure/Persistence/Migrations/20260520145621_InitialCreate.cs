using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyDel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recently_played",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recently_played", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "spotify_tokens",
                columns: table => new
                {
                    UserSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spotify_tokens", x => x.UserSessionId);
                    table.ForeignKey(
                        name: "FK_spotify_tokens_user_sessions_UserSessionId",
                        column: x => x.UserSessionId,
                        principalTable: "user_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recently_played_UserSessionId_TrackId_PlayedAt",
                table: "recently_played",
                columns: new[] { "UserSessionId", "TrackId", "PlayedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_SpotifyUserId",
                table: "user_sessions",
                column: "SpotifyUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recently_played");

            migrationBuilder.DropTable(
                name: "spotify_tokens");

            migrationBuilder.DropTable(
                name: "user_sessions");
        }
    }
}
