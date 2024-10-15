using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class movementclassnull20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MovementClassId",
                table: "Movements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(6990), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(6994) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(6998), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(6998) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(6999), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7000) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7001), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7001) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7002), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7003) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7003), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7004) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7005), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7005) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7006), new DateTime(2024, 10, 15, 17, 48, 0, 9, DateTimeKind.Utc).AddTicks(7007) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MovementClassId",
                table: "Movements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5985), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5989) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5994), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5995) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5996), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5996) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5997), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5998) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(5999), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6000) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6001), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6001) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6002), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6002) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6003), new DateTime(2024, 10, 15, 17, 46, 59, 363, DateTimeKind.Utc).AddTicks(6004) });
        }
    }
}
