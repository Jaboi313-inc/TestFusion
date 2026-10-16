using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestFusion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TimeOffTesting = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PartNumber = table.Column<string>(type: "text", nullable: false),
                    PartBrand = table.Column<string>(type: "text", nullable: false),
                    PartType = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    CustomerMail = table.Column<string>(type: "text", nullable: false),
                    CustomerNotes = table.Column<string>(type: "text", nullable: false),
                    TestNotes = table.Column<string>(type: "text", nullable: false),
                    InjectorSerialNumber = table.Column<string>(type: "text", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_CustomerMail",
                table: "test_reports",
                column: "CustomerMail");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_CustomerName",
                table: "test_reports",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_InjectorSerialNumber",
                table: "test_reports",
                column: "InjectorSerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_PartBrand",
                table: "test_reports",
                column: "PartBrand");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_PartNumber",
                table: "test_reports",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_PartType",
                table: "test_reports",
                column: "PartType");

            migrationBuilder.CreateIndex(
                name: "IX_test_reports_TimeOffTesting",
                table: "test_reports",
                column: "TimeOffTesting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_reports");
        }
    }
}
