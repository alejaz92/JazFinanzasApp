using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionddetransactionclassatransactionClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentTransactions_Transactions_ExpenseTransactionId",
                table: "InvestmentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentTransactions_Transactions_IncomeTransactionId",
                table: "InvestmentTransactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9770), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9774) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9777), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9777) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9778), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9779) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9780), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9780) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9781), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9781) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9782), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9782) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9783), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9784) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9784), new DateTime(2024, 11, 25, 14, 22, 4, 463, DateTimeKind.Utc).AddTicks(9785) });

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentTransactions_Transactions_ExpenseTransactionId",
                table: "InvestmentTransactions",
                column: "ExpenseTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentTransactions_Transactions_IncomeTransactionId",
                table: "InvestmentTransactions",
                column: "IncomeTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentTransactions_Transactions_ExpenseTransactionId",
                table: "InvestmentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentTransactions_Transactions_IncomeTransactionId",
                table: "InvestmentTransactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2804), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2807) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2810), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2811) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2812), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2812) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2813), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2813) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2814), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2815) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2815), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2816) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2817), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2817) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2818), new DateTime(2024, 11, 13, 14, 35, 18, 642, DateTimeKind.Utc).AddTicks(2818) });

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentTransactions_Transactions_ExpenseTransactionId",
                table: "InvestmentTransactions",
                column: "ExpenseTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentTransactions_Transactions_IncomeTransactionId",
                table: "InvestmentTransactions",
                column: "IncomeTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }
    }
}
