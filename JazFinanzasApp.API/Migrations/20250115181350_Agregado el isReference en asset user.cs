using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoelisReferenceenassetuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isReference",
                table: "Assets_Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isReference",
                table: "Assets_Users");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(694), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(697) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(700), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(700) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(701), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(701) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(702), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(702) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(703), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(703) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(704), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(705) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(705), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(706) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(706), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(707) });
        }
    }
}
