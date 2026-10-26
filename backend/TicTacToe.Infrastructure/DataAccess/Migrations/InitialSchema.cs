using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TicTacToe.Infrastructure;

namespace TicTacToe.Infrastructure.DataAccess.Migrations;

[DbContext(typeof(GameDbContext))]
[Migration("202610260001_InitialSchema")]
public sealed class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Games",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Games", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Scoreboards",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                XWins = table.Column<int>(type: "int", nullable: false),
                OWins = table.Column<int>(type: "int", nullable: false),
                Draws = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Scoreboards", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Username));

        migrationBuilder.CreateIndex(name: "IX_Games_UpdatedAt", table: "Games", column: "UpdatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Games");
        migrationBuilder.DropTable(name: "Scoreboards");
        migrationBuilder.DropTable(name: "Users");
    }
}
