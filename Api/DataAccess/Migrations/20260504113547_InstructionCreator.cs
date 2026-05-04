using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InstructionCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CommentId",
                table: "Instructions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Instructions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Instructions_CommentId",
                table: "Instructions",
                column: "CommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instructions_Comments_CommentId",
                table: "Instructions",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "CommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instructions_Comments_CommentId",
                table: "Instructions");

            migrationBuilder.DropIndex(
                name: "IX_Instructions_CommentId",
                table: "Instructions");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Instructions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Instructions");
        }
    }
}
