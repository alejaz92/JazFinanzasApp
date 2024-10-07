using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class Agregadotablaaccountassettype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6763), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6766) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6770), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6770) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6771), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6771) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6772), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6773) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6773), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6774) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6775), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6775) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6776), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6776) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6777), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6777) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1067), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1071) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1075), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1075) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1076), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1076) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1077), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1078) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1079), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1079) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1080), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1080) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1081), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1081) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1082), new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1082) });
        }
    }
}
