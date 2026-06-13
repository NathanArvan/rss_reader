using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RssReader.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsAndItemTriageIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastOpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "LastOpenedUtc" },
                values: new object[] { 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TriageState_SourceId",
                table: "Items",
                columns: new[] { "TriageState", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropIndex(
                name: "IX_Items_TriageState_SourceId",
                table: "Items");
        }
    }
}
