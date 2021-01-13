using Microsoft.EntityFrameworkCore.Migrations;

namespace HyperBot.Migrations
{
    public partial class Prefixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prefixes",
                columns: table => new
                {
                    PrefixText = table.Column<string>(type: "TEXT", nullable: false),
                    Guild = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prefixes", x => new { x.PrefixText, x.Guild });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prefixes");
        }
    }
}
