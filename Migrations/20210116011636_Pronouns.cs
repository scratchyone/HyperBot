using Microsoft.EntityFrameworkCore.Migrations;

namespace HyperBot.Migrations
{
    public partial class Pronouns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pronouns",
                columns: table => new
                {
                    Set = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pronouns", x => x.Set);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pronouns");
        }
    }
}
