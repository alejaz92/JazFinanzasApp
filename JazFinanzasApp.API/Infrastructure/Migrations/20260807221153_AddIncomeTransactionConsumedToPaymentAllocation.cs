using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeTransactionConsumedToPaymentAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncomeTransactionConsumed",
                table: "SharedEventPaymentAllocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7821), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7823) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7826), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7826) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7827), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7827) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7828), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7828) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7829), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7829) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7830), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7830) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7831), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7831) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7832), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7832) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncomeTransactionConsumed",
                table: "SharedEventPaymentAllocations");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2876), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2879) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2882), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2883), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2884), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2884) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2885), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2885) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2886), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2886) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2887), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2887) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2888), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2888) });
        }
    }
}
