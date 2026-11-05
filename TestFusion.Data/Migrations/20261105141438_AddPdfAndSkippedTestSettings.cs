using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestFusion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfAndSkippedTestSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PdfDefaultLayoutMode",
                table: "application_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowFullySkippedTests",
                table: "application_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfDefaultLayoutMode",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "ShowFullySkippedTests",
                table: "application_settings");
        }
    }
}
