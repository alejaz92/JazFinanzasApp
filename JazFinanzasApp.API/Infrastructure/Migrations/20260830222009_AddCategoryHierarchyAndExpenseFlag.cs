using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryHierarchyAndExpenseFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CountsAsIncomeExpense",
                table: "TransactionClasses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TransactionClasses",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2290), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2293) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2296), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2297) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2298), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2298) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2299), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2299) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2300), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2300) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2301), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2301) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2302), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2302) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2303), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2303) });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionClasses_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId",
                principalTable: "TransactionClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionClasses_TransactionClasses_ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropIndex(
                name: "IX_TransactionClasses_ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "CountsAsIncomeExpense",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TransactionClasses");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9213), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9215) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9218), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9219) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9219), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9220) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9220), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9221) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9221), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9222) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9222), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9223) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9223), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9224) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9224), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9225) });
        }
    }
}
