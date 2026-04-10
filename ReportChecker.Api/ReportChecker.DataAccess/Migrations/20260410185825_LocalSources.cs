using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class LocalSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalCheckSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalCheckSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalCheckSources_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "CheckId");
                });

            migrationBuilder.CreateTable(
                name: "LocalReportSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryFilePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientMachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalReportSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalReportSources_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCheckSources_CheckId",
                table: "LocalCheckSources",
                column: "CheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalReportSources_ReportId",
                table: "LocalReportSources",
                column: "ReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalCheckSources");

            migrationBuilder.DropTable(
                name: "LocalReportSources");
        }
    }
}
