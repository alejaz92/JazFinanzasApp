using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class seagregocoloraassets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Assets");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7568), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7572) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7576), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7576) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7577), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7577) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7578), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7578) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7579), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7579) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7580), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7580) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7581), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7581) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7582), new DateTime(2025, 1, 15, 18, 13, 49, 337, DateTimeKind.Utc).AddTicks(7582) });
        }
    }
}
