using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class Updatedatefieldincardmovement2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastInstallment",
                table: "CardTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4027), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4033) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4038), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4039) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4041), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4042) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4046), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4047) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastInstallment",
                table: "CardTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

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
    }
}
