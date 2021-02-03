using Microsoft.EntityFrameworkCore.Migrations;

namespace HyperBot.Migrations
{
    public partial class ServerProtectData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPGrabberUrls",
                columns: table => new
                {
                    Domain = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPGrabberUrls", x => x.Domain);
                });

            migrationBuilder.CreateTable(
                name: "UnsafeFiles",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnsafeFiles", x => x.Hash);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPGrabberUrls");

            migrationBuilder.DropTable(
                name: "UnsafeFiles");
        }
    }
}
