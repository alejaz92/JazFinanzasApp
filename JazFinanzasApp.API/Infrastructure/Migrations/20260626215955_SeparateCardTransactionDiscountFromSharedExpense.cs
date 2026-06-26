using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateCardTransactionDiscountFromSharedExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardTransactionDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardTransactionId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTransactionDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTransactionDiscounts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CardTransactionDiscounts_CardTransactions_CardTransactionId",
                        column: x => x.CardTransactionId,
                        principalTable: "CardTransactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CardTransactionDiscountInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardTransactionDiscountId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTransactionDiscountInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTransactionDiscountInstallments_CardTransactionDiscounts_CardTransactionDiscountId",
                        column: x => x.CardTransactionDiscountId,
                        principalTable: "CardTransactionDiscounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CardTransactionDiscountInstallments_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            // Preservar los SharedExpenseSplit/SharedExpenseReimbursement de tipo BankPromotion (SplitType = 1)
            // moviéndolos a las tablas nuevas antes de borrar las columnas/filas viejas.
            // El join por CardTransactionId funciona porque CardTransactionDiscount es 1:1 con CardTransaction.
            migrationBuilder.Sql(@"
                INSERT INTO CardTransactionDiscounts (CardTransactionId, Amount, AmountApplied, Notes, UserId, CreatedAt, UpdatedAt)
                SELECT se.CardTransactionId, s.Amount, 0, s.Notes, se.UserId, s.CreatedAt, s.UpdatedAt
                FROM SharedExpenseSplits s
                JOIN SharedExpenses se ON se.Id = s.SharedExpenseId
                WHERE s.SplitType = 1 AND se.CardTransactionId IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO CardTransactionDiscountInstallments (CardTransactionDiscountId, TransactionId, Amount, InstallmentNumber, Date, CreatedAt, UpdatedAt)
                SELECT ctd.Id, r.TransactionId, r.Amount, r.InstallmentNumber, r.Date, r.CreatedAt, r.UpdatedAt
                FROM SharedExpenseReimbursements r
                JOIN SharedExpenseSplits s ON s.Id = r.SharedExpenseSplitId
                JOIN SharedExpenses se ON se.Id = s.SharedExpenseId
                JOIN CardTransactionDiscounts ctd ON ctd.CardTransactionId = se.CardTransactionId
                WHERE s.SplitType = 1 AND r.InstallmentNumber IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                DELETE r
                FROM SharedExpenseReimbursements r
                JOIN SharedExpenseSplits s ON s.Id = r.SharedExpenseSplitId
                WHERE s.SplitType = 1 AND r.InstallmentNumber IS NOT NULL;
            ");

            migrationBuilder.Sql(@"DELETE FROM SharedExpenseSplits WHERE SplitType = 1;");

            // Borra los SharedExpense que quedaron sin ningún split tras extraer el de promoción
            // (si no, bloquean para siempre un futuro gasto compartido real sobre esa CardTransaction).
            migrationBuilder.Sql(@"
                DELETE se
                FROM SharedExpenses se
                WHERE NOT EXISTS (SELECT 1 FROM SharedExpenseSplits s WHERE s.SharedExpenseId = se.Id);
            ");

            migrationBuilder.DropColumn(
                name: "SplitType",
                table: "SharedExpenseSplits");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "SharedExpenseReimbursements");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "SharedExpenseSplits",
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
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5271), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5274) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5328), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5329) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5330), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5330) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5331), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5331) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5332), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5332) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5333), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5333) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5334), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5334) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5335), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5335) });

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactionDiscountInstallments_CardTransactionDiscountId",
                table: "CardTransactionDiscountInstallments",
                column: "CardTransactionDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactionDiscountInstallments_TransactionId",
                table: "CardTransactionDiscountInstallments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactionDiscounts_CardTransactionId",
                table: "CardTransactionDiscounts",
                column: "CardTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactionDiscounts_UserId",
                table: "CardTransactionDiscounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardTransactionDiscountInstallments");

            migrationBuilder.DropTable(
                name: "CardTransactionDiscounts");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "SharedExpenseSplits",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SplitType",
                table: "SharedExpenseSplits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "SharedExpenseReimbursements",
                type: "int",
                nullable: true);

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
        }
    }
}
