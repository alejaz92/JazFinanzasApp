using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class Updatedatefieldincardmovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateMovement",
                table: "CardMovements",
                newName: "Date");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2000), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2007) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2011), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2011) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2012), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2013) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2014), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2014) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2015), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2015) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2016), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2017) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2018), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2018) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2019), new DateTime(2024, 10, 25, 15, 44, 44, 338, DateTimeKind.Utc).AddTicks(2019) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "CardMovements",
                newName: "DateMovement");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1232), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1236) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1239), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1239) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1240), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1241) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1241), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1242) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1242), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1243) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1243), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1244) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1245), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1245) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1246), new DateTime(2024, 10, 23, 17, 39, 13, 776, DateTimeKind.Utc).AddTicks(1246) });
        }
    }
}
