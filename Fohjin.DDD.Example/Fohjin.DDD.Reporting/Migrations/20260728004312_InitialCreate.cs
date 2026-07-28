using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fohjin.DDD.Reporting.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountDetailsReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountName = table.Column<string>(type: "TEXT", nullable: true),
                Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountDetailsReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AccountReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientDetailsReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountName = table.Column<string>(type: "TEXT", nullable: true),
                AccountNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ClientDetailsReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientName = table.Column<string>(type: "TEXT", nullable: true),
                Street = table.Column<string>(type: "TEXT", nullable: true),
                StreetNumber = table.Column<string>(type: "TEXT", nullable: true),
                PostalCode = table.Column<string>(type: "TEXT", nullable: true),
                City = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientDetailsReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ClientReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ClosedAccountDetailsReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountName = table.Column<string>(type: "TEXT", nullable: true),
                Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                AccountNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClosedAccountDetailsReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ClosedAccountReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClientDetailsReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountName = table.Column<string>(type: "TEXT", nullable: true),
                AccountNumber = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClosedAccountReport", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LedgerReport",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountDetailsReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                Action = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerReport", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AccountDetailsReport");

        migrationBuilder.DropTable(
            name: "AccountReport");

        migrationBuilder.DropTable(
            name: "ClientDetailsReport");

        migrationBuilder.DropTable(
            name: "ClientReport");

        migrationBuilder.DropTable(
            name: "ClosedAccountDetailsReport");

        migrationBuilder.DropTable(
            name: "ClosedAccountReport");

        migrationBuilder.DropTable(
            name: "LedgerReport");
    }
}
