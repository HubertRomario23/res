using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResultViewer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestResultOutput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Output",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Output",
                table: "TestResults");
        }
    }
}
