using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionClassIsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "TransactionClasses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1158), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1160) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1164), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1164) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1165), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1165) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1166), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1166) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1167), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1167) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1168), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1169) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1169), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1170) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1170), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1171) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "TransactionClasses");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1192), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1194) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1197), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1198) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1198), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1199) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1201) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1201), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1202) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1202), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1203) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1203), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1204) });
        }
    }
}
