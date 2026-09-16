using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8082), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8084) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8090), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8090) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8091), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8091) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8092), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8093) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8093), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8094) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8095), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8095) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8096), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8096) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8097), new DateTime(2026, 9, 16, 23, 10, 35, 983, DateTimeKind.Utc).AddTicks(8097) });

            migrationBuilder.CreateIndex(
                name: "IX_ReportFavorites_UserId",
                table: "ReportFavorites",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportFavorites");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1485), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1488) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1492), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1493) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1494), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1494) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1495), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1495) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1496), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1496) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1497), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1497) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1498), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1498) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1499), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1499) });
        }
    }
}
