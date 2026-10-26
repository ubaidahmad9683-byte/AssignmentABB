using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TicTacToe.Infrastructure;

namespace TicTacToe.Infrastructure.DataAccess.Migrations;

[DbContext(typeof(GameDbContext))]
[Migration("202610260002_AddProducts")]
public sealed class AddProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Stock = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Products", product => product.Id));

        migrationBuilder.CreateIndex("IX_Products_Name", "Products", "Name");
        migrationBuilder.CreateIndex("IX_Products_Price", "Products", "Price");
        migrationBuilder.CreateIndex("IX_Products_CreatedAt", "Products", "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Products");
}
