using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YARL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAndRomFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RawTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PlatformId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_RomSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "RomSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RomFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    CRC32Hash = table.Column<string>(type: "TEXT", nullable: true),
                    MD5Hash = table.Column<string>(type: "TEXT", nullable: true),
                    SHA1Hash = table.Column<string>(type: "TEXT", nullable: true),
                    DiscNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    IsM3uPlaylist = table.Column<bool>(type: "INTEGER", nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomFiles_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RomFiles_RomSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "RomSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_LastPlayedAt",
                table: "Games",
                column: "LastPlayedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlatformId_Status",
                table: "Games",
                columns: new[] { "PlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_SourceId",
                table: "Games",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_FilePath",
                table: "RomFiles",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_GameId",
                table: "RomFiles",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RomFiles_SourceId",
                table: "RomFiles",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RomFiles");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
