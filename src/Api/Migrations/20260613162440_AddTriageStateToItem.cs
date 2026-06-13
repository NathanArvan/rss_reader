using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RssReader.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageStateToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TriageState",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriageState",
                table: "Items");
        }
    }
}
