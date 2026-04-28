using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportChecker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImageProcessingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageProcessingMode",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageProcessingMode",
                table: "Reports");
        }
    }
}
