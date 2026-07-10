using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5872), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5875) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5879), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5879) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5880), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5880) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5881), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5881) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5882), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5883), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5884), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5884) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5885), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5885) });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserId",
                table: "Trips",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9863), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9873) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9876), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9876) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9880), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9880) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9881), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9881) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9882), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9883), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9884), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9885) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9885), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9886) });
        }
    }
}
