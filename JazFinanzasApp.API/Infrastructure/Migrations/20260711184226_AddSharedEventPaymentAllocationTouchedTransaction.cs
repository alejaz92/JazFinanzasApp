using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedEventPaymentAllocationTouchedTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TouchedTransactionId",
                table: "SharedEventPaymentAllocations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3992), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3994) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3997), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3997) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3998), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3998) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3999), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3999) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4000), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4000) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4001), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4001) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4002), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4002) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4003), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4003) });

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_TouchedTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "TouchedTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedEventPaymentAllocations_Transactions_TouchedTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "TouchedTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedEventPaymentAllocations_Transactions_TouchedTransactionId",
                table: "SharedEventPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_SharedEventPaymentAllocations_TouchedTransactionId",
                table: "SharedEventPaymentAllocations");

            migrationBuilder.DropColumn(
                name: "TouchedTransactionId",
                table: "SharedEventPaymentAllocations");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4255), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4258) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4261), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4261) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4262), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4262) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4263), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4263) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4264), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4264) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4265), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4265) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4266), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4266) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4267), new DateTime(2026, 7, 11, 17, 48, 20, 826, DateTimeKind.Utc).AddTicks(4267) });
        }
    }
}
