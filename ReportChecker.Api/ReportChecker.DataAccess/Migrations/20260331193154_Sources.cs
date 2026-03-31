using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Sources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileCheckSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileCheckSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileCheckSources_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "CheckId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileReportSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryFilePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileReportSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileReportSources_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitHubCheckSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubCheckSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitHubCheckSources_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "CheckId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitHubReportSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<long>(type: "bigint", nullable: false),
                    Branch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubReportSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitHubReportSources_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileCheckSources_CheckId",
                table: "FileCheckSources",
                column: "CheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileReportSources_ReportId",
                table: "FileReportSources",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubCheckSources_CheckId",
                table: "GitHubCheckSources",
                column: "CheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubReportSources_ReportId",
                table: "GitHubReportSources",
                column: "ReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileCheckSources");

            migrationBuilder.DropTable(
                name: "FileReportSources");

            migrationBuilder.DropTable(
                name: "GitHubCheckSources");

            migrationBuilder.DropTable(
                name: "GitHubReportSources");
        }
    }
}
