using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SourceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileCheckSources_Checks_CheckId",
                table: "FileCheckSources");

            migrationBuilder.DropForeignKey(
                name: "FK_GitHubCheckSources_Checks_CheckId",
                table: "GitHubCheckSources");

            migrationBuilder.AlterColumn<Guid>(
                name: "CheckId",
                table: "GitHubCheckSources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "FileCheckSources",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CheckId",
                table: "FileCheckSources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_FileCheckSources_Checks_CheckId",
                table: "FileCheckSources",
                column: "CheckId",
                principalTable: "Checks",
                principalColumn: "CheckId");

            migrationBuilder.AddForeignKey(
                name: "FK_GitHubCheckSources_Checks_CheckId",
                table: "GitHubCheckSources",
                column: "CheckId",
                principalTable: "Checks",
                principalColumn: "CheckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileCheckSources_Checks_CheckId",
                table: "FileCheckSources");

            migrationBuilder.DropForeignKey(
                name: "FK_GitHubCheckSources_Checks_CheckId",
                table: "GitHubCheckSources");

            migrationBuilder.AlterColumn<Guid>(
                name: "CheckId",
                table: "GitHubCheckSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "FileCheckSources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CheckId",
                table: "FileCheckSources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FileCheckSources_Checks_CheckId",
                table: "FileCheckSources",
                column: "CheckId",
                principalTable: "Checks",
                principalColumn: "CheckId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GitHubCheckSources_Checks_CheckId",
                table: "GitHubCheckSources",
                column: "CheckId",
                principalTable: "Checks",
                principalColumn: "CheckId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
