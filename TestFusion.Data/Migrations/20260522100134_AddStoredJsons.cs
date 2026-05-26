using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestFusion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredJsons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stored_jsons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stored_jsons", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stored_jsons");
        }
    }
}
