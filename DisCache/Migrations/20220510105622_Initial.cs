using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DisCache.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActiveFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyExceptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyExceptions_ActiveFrom",
                table: "KeyExceptions",
                column: "ActiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_KeyExceptions_ActiveTo",
                table: "KeyExceptions",
                column: "ActiveTo");

            migrationBuilder.CreateIndex(
                name: "IX_KeyExceptions_IsActive",
                table: "KeyExceptions",
                column: "IsActive");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyExceptions");
        }
    }
}
