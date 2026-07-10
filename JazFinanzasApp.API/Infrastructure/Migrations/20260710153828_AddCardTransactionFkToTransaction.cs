using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardTransactionFkToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardTransactionId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8814), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8816) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8820), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8820) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8821), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8821) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8822), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8822) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8823), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8823) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8824), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8824) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8825), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8825) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8826), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8826) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CardTransactionId",
                table: "Transactions",
                column: "CardTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_CardTransactions_CardTransactionId",
                table: "Transactions",
                column: "CardTransactionId",
                principalTable: "CardTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_CardTransactions_CardTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CardTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CardTransactionId",
                table: "Transactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5872), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5875) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5879), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5879) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5880), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5880) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5881), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5881) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5882), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5883), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5884), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5884) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5885), new DateTime(2026, 7, 10, 15, 31, 14, 429, DateTimeKind.Utc).AddTicks(5885) });
        }
    }
}
