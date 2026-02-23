using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResultViewer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRunSpecflowLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecflowLog",
                table: "TestRuns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecflowLog",
                table: "TestRuns");
        }
    }
}
