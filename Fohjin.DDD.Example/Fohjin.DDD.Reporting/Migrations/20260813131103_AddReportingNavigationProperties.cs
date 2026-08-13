using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fohjin.DDD.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LedgerReport_AccountDetailsReportId",
                table: "LedgerReport",
                column: "AccountDetailsReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCardReport_ClientDetailsReportId",
                table: "BankCardReport",
                column: "ClientDetailsReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountReport_ClientDetailsReportId",
                table: "AccountReport",
                column: "ClientDetailsReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountReport_ClientDetailsReport_ClientDetailsReportId",
                table: "AccountReport",
                column: "ClientDetailsReportId",
                principalTable: "ClientDetailsReport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BankCardReport_ClientDetailsReport_ClientDetailsReportId",
                table: "BankCardReport",
                column: "ClientDetailsReportId",
                principalTable: "ClientDetailsReport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerReport_AccountDetailsReport_AccountDetailsReportId",
                table: "LedgerReport",
                column: "AccountDetailsReportId",
                principalTable: "AccountDetailsReport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountReport_ClientDetailsReport_ClientDetailsReportId",
                table: "AccountReport");

            migrationBuilder.DropForeignKey(
                name: "FK_BankCardReport_ClientDetailsReport_ClientDetailsReportId",
                table: "BankCardReport");

            migrationBuilder.DropForeignKey(
                name: "FK_LedgerReport_AccountDetailsReport_AccountDetailsReportId",
                table: "LedgerReport");

            migrationBuilder.DropIndex(
                name: "IX_LedgerReport_AccountDetailsReportId",
                table: "LedgerReport");

            migrationBuilder.DropIndex(
                name: "IX_BankCardReport_ClientDetailsReportId",
                table: "BankCardReport");

            migrationBuilder.DropIndex(
                name: "IX_AccountReport_ClientDetailsReportId",
                table: "AccountReport");
        }
    }
}
