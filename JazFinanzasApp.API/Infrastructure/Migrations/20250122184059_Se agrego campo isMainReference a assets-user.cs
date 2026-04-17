using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class SeagregocampoisMainReferenceaassetsuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isMainReference",
                table: "Assets_Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9481), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9483) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9489), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9489) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9490), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9490) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9491), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9492) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9493), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9493) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9494), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9494) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9495), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9495) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9496), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9496) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isMainReference",
                table: "Assets_Users");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6205), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6207) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6212), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6212) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6214), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6214) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6216), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6216) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6217), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6218) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6219), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6219) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6221), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6221) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6222), new DateTime(2025, 1, 16, 14, 49, 59, 591, DateTimeKind.Utc).AddTicks(6222) });
        }
    }
}
