using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Benchmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpectedCount = table.Column<int>(type: "integer", nullable: false),
                    FoundCount = table.Column<int>(type: "integer", nullable: false),
                    MatchedCount = table.Column<int>(type: "integer", nullable: false),
                    TitleMatchCount = table.Column<int>(type: "integer", nullable: false),
                    PriorityMatchCount = table.Column<int>(type: "integer", nullable: false),
                    FixCheckedCount = table.Column<int>(type: "integer", nullable: false),
                    FixMatchCount = table.Column<int>(type: "integer", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalRequests = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkRuns_LlmModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "LlmModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedNumber = table.Column<int>(type: "integer", nullable: true),
                    ErrorClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Chapter = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Line = table.Column<int>(type: "integer", nullable: true),
                    ExpectedTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpectedComment = table.Column<string>(type: "text", nullable: true),
                    ExpectedPriority = table.Column<int>(type: "integer", nullable: true),
                    FoundIndex = table.Column<int>(type: "integer", nullable: true),
                    FoundTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FoundComment = table.Column<string>(type: "text", nullable: true),
                    FoundPriority = table.Column<int>(type: "integer", nullable: true),
                    IsFound = table.Column<bool>(type: "boolean", nullable: false),
                    TitleMatch = table.Column<bool>(type: "boolean", nullable: true),
                    PriorityMatch = table.Column<bool>(type: "boolean", nullable: true),
                    PriorityDelta = table.Column<int>(type: "integer", nullable: true),
                    MatchingMethod = table.Column<int>(type: "integer", nullable: false),
                    MatchScore = table.Column<double>(type: "double precision", nullable: true),
                    MatchingReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FixMatchStatus = table.Column<int>(type: "integer", nullable: false),
                    ExpectedFix = table.Column<string>(type: "text", nullable: true),
                    FoundFix = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkResults_BenchmarkRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "BenchmarkRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResults_RunId",
                table: "BenchmarkResults",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_CaseId_ModelId_CreatedAt",
                table: "BenchmarkRuns",
                columns: new[] { "CaseId", "ModelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_DeletedAt",
                table: "BenchmarkRuns",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_ModelId",
                table: "BenchmarkRuns",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkRuns_Status",
                table: "BenchmarkRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkResults");

            migrationBuilder.DropTable(
                name: "BenchmarkRuns");
        }
    }
}
