using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResultViewer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Pdc = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RunId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallResult = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TestCount = table.Column<int>(type: "int", nullable: false),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchivePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    MeasurementName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MeasuredValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SpecErrorUpper = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpecErrorLower = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpecWarningUpper = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SpecWarningLower = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_TestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "TestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    STM = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MSIVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PDCVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MonoplaneOrBiplane = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FrontalStandType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TableType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TableTopType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DetectorNameFrontal = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DetectorNameLateral = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SystemType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProductFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DetectorType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LateralStandType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SystemConfigType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RawXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemInfos_TestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "TestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    TestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_TestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "TestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_TestRunId",
                table: "Measurements",
                column: "TestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemInfos_TestRunId",
                table: "SystemInfos",
                column: "TestRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestRunId",
                table: "TestResults",
                column: "TestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_CreatedAt",
                table: "TestRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_Host_Pdc_RunId",
                table: "TestRuns",
                columns: new[] { "Host", "Pdc", "RunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_IsDeleted",
                table: "TestRuns",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_OverallResult",
                table: "TestRuns",
                column: "OverallResult");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_StartTime",
                table: "TestRuns",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "SystemInfos");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestRuns");
        }
    }
}
