using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TestFusion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowDebugInformation = table.Column<bool>(type: "boolean", nullable: false),
                    GroupTestsByBaseName = table.Column<bool>(type: "boolean", nullable: false),
                    MatchCustomerName = table.Column<bool>(type: "boolean", nullable: false),
                    MatchCustomerPhone = table.Column<bool>(type: "boolean", nullable: false),
                    MatchCustomerMail = table.Column<bool>(type: "boolean", nullable: false),
                    MatchCustomerNotes = table.Column<bool>(type: "boolean", nullable: false),
                    PdfMaxInjectorsPerPage = table.Column<int>(type: "integer", nullable: false),
                    PlaywrightEmailEncrypted = table.Column<string>(type: "text", nullable: true),
                    PlaywrightPasswordEncrypted = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_settings");
        }
    }
}
