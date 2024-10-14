using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class agregadacolleciondeassetsaassettype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetTypeId1",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4606), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4610) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4613), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4613) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4614), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4615) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4615), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4616) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4617), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4617) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4617), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4618) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4618), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4619) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4620), new DateTime(2024, 10, 14, 14, 16, 8, 975, DateTimeKind.Utc).AddTicks(4620) });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeId1",
                table: "Assets",
                column: "AssetTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetTypes_AssetTypeId1",
                table: "Assets",
                column: "AssetTypeId1",
                principalTable: "AssetTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetTypes_AssetTypeId1",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_AssetTypeId1",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "AssetTypeId1",
                table: "Assets");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2112), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2116) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2119), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2119) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2120), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2121) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2122), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2122) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2123), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2123) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2124), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2124) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2125), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2125) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2126), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2126) });
        }
    }
}
