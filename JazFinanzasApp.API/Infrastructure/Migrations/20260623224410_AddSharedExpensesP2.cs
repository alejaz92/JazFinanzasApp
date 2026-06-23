using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedExpensesP2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "SharedExpenseSplits",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountApplied",
                table: "SharedExpenseSplits",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InstallmentSplitAmount",
                table: "SharedExpenseSplits",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SplitType",
                table: "SharedExpenseSplits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TransactionId",
                table: "SharedExpenses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CardTransactionId",
                table: "SharedExpenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SharedExpenseReimbursements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedExpenseSplitId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedExpenseReimbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedExpenseReimbursements_SharedExpenseSplits_SharedExpenseSplitId",
                        column: x => x.SharedExpenseSplitId,
                        principalTable: "SharedExpenseSplits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedExpenseReimbursements_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1342), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1343) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1348), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1348) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1349), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1349) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1350), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1351) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1351), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1352) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1352), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1353) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1353), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1354) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1354), new DateTime(2026, 6, 23, 22, 44, 9, 769, DateTimeKind.Utc).AddTicks(1355) });

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_CardTransactionId",
                table: "SharedExpenses",
                column: "CardTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenseReimbursements_SharedExpenseSplitId",
                table: "SharedExpenseReimbursements",
                column: "SharedExpenseSplitId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenseReimbursements_TransactionId",
                table: "SharedExpenseReimbursements",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedExpenses_CardTransactions_CardTransactionId",
                table: "SharedExpenses",
                column: "CardTransactionId",
                principalTable: "CardTransactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedExpenses_CardTransactions_CardTransactionId",
                table: "SharedExpenses");

            migrationBuilder.DropTable(
                name: "SharedExpenseReimbursements");

            migrationBuilder.DropIndex(
                name: "IX_SharedExpenses_CardTransactionId",
                table: "SharedExpenses");

            migrationBuilder.DropColumn(
                name: "AmountApplied",
                table: "SharedExpenseSplits");

            migrationBuilder.DropColumn(
                name: "InstallmentSplitAmount",
                table: "SharedExpenseSplits");

            migrationBuilder.DropColumn(
                name: "SplitType",
                table: "SharedExpenseSplits");

            migrationBuilder.DropColumn(
                name: "CardTransactionId",
                table: "SharedExpenses");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "SharedExpenseSplits",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TransactionId",
                table: "SharedExpenses",
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
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1158), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1160) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1164), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1164) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1165), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1165) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1166), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1166) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1167), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1167) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1168), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1169) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1169), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1170) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1170), new DateTime(2026, 6, 23, 22, 23, 5, 888, DateTimeKind.Utc).AddTicks(1171) });
        }
    }
}
