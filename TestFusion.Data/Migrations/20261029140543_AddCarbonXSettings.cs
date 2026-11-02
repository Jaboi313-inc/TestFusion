using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestFusion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarbonXSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlaywrightPasswordEncrypted",
                table: "application_settings",
                newName: "CarbonXPasswordEncrypted");

            migrationBuilder.RenameColumn(
                name: "PlaywrightEmailEncrypted",
                table: "application_settings",
                newName: "CarbonXEmailEncrypted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CarbonXPasswordEncrypted",
                table: "application_settings",
                newName: "PlaywrightPasswordEncrypted");

            migrationBuilder.RenameColumn(
                name: "CarbonXEmailEncrypted",
                table: "application_settings",
                newName: "PlaywrightEmailEncrypted");
        }
    }
}
