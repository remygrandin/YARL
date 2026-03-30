using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YARL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalPlayTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalPlayTime",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPlayTime",
                table: "Games");
        }
    }
}
