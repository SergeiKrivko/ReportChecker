using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class YandexDiskSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YandexDiskAccounts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AccessToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YandexDiskAccounts", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "YandexDiskCheckSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YandexDiskCheckSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YandexDiskCheckSources_Checks_CheckId",
                        column: x => x.CheckId,
                        principalTable: "Checks",
                        principalColumn: "CheckId");
                });

            migrationBuilder.CreateTable(
                name: "YandexDiskReportSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EntryFilePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YandexDiskReportSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YandexDiskReportSources_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YandexDiskCheckSources_CheckId",
                table: "YandexDiskCheckSources",
                column: "CheckId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YandexDiskReportSources_ReportId",
                table: "YandexDiskReportSources",
                column: "ReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YandexDiskAccounts");

            migrationBuilder.DropTable(
                name: "YandexDiskCheckSources");

            migrationBuilder.DropTable(
                name: "YandexDiskReportSources");
        }
    }
}
