using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fohjin.DDD.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class MergeAccountReportStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClosedAccountDetailsReport");

            migrationBuilder.DropTable(
                name: "ClosedAccountReport");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AccountReport",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AccountDetailsReport",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Open");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AccountReport");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AccountDetailsReport");

            migrationBuilder.CreateTable(
                name: "ClosedAccountDetailsReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClientReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosedAccountDetailsReport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClosedAccountReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientDetailsReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsertionSequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosedAccountReport", x => x.Id);
                });
        }
    }
}
