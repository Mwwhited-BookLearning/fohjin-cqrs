using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fohjin.DDD.EventStore.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventProviders",
                columns: table => new
                {
                    EventProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProviders", x => x.EventProviderId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Event = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnapShots",
                columns: table => new
                {
                    EventProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapShot = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapShots", x => x.EventProviderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventProviderId_Version",
                table: "Events",
                columns: new[] { "EventProviderId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventProviders");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "SnapShots");
        }
    }
}
