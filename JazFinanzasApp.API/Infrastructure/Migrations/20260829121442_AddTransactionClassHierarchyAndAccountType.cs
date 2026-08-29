using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionClassHierarchyAndAccountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nature",
                table: "TransactionClasses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TransactionClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsLiquid",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8144), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8146) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8149), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8150) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8151), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8151) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8152), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8152) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8152), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8153) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8153), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8154) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8154), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8155) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8155), new DateTime(2026, 8, 29, 12, 14, 41, 473, DateTimeKind.Utc).AddTicks(8156) });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionClasses_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId",
                principalTable: "TransactionClasses",
                principalColumn: "Id");
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
                name: "Nature",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "CountsAsLiquid",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Accounts");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9530), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9532) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9535), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9535) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9536), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9537) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9538), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9538) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9539), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9539) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9540), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9540) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9541), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9542) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9542), new DateTime(2026, 8, 26, 20, 30, 28, 764, DateTimeKind.Utc).AddTicks(9543) });
        }
    }
}
