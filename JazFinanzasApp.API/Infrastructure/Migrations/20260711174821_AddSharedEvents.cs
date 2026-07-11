using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedEventMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedEventId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionClassId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayerPersonId = table.Column<int>(type: "int", nullable: true),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    CardTransactionId = table.Column<int>(type: "int", nullable: true),
                    SharedExpenseId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEventMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_CardTransactions_CardTransactionId",
                        column: x => x.CardTransactionId,
                        principalTable: "CardTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_People_PayerPersonId",
                        column: x => x.PayerPersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_SharedEvents_SharedEventId",
                        column: x => x.SharedEventId,
                        principalTable: "SharedEvents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_SharedExpenses_SharedExpenseId",
                        column: x => x.SharedExpenseId,
                        principalTable: "SharedExpenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_TransactionClasses_TransactionClassId",
                        column: x => x.TransactionClassId,
                        principalTable: "TransactionClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovements_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedEventParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedEventId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEventParticipants_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventParticipants_SharedEvents_SharedEventId",
                        column: x => x.SharedEventId,
                        principalTable: "SharedEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedEventPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedEventId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FromPersonId = table.Column<int>(type: "int", nullable: true),
                    ToPersonId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    IsInternalCompensation = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEventPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_People_FromPersonId",
                        column: x => x.FromPersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_People_ToPersonId",
                        column: x => x.ToPersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPayments_SharedEvents_SharedEventId",
                        column: x => x.SharedEventId,
                        principalTable: "SharedEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedEventMovementShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedEventMovementId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountSettled = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SharedExpenseSplitId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEventMovementShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEventMovementShares_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovementShares_SharedEventMovements_SharedEventMovementId",
                        column: x => x.SharedEventMovementId,
                        principalTable: "SharedEventMovements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventMovementShares_SharedExpenseSplits_SharedExpenseSplitId",
                        column: x => x.SharedExpenseSplitId,
                        principalTable: "SharedExpenseSplits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedEventPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedEventPaymentId = table.Column<int>(type: "int", nullable: false),
                    SharedExpenseSplitId = table.Column<int>(type: "int", nullable: true),
                    SharedEventMovementShareId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedExpenseTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedIncomeTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedExchangeOutTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedExchangeInTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedEventPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_SharedEventMovementShares_SharedEventMovementShareId",
                        column: x => x.SharedEventMovementShareId,
                        principalTable: "SharedEventMovementShares",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_SharedEventPayments_SharedEventPaymentId",
                        column: x => x.SharedEventPaymentId,
                        principalTable: "SharedEventPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_SharedExpenseSplits_SharedExpenseSplitId",
                        column: x => x.SharedExpenseSplitId,
                        principalTable: "SharedExpenseSplits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_Transactions_CreatedExchangeInTransactionId",
                        column: x => x.CreatedExchangeInTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_Transactions_CreatedExchangeOutTransactionId",
                        column: x => x.CreatedExchangeOutTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_Transactions_CreatedExpenseTransactionId",
                        column: x => x.CreatedExpenseTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedEventPaymentAllocations_Transactions_CreatedIncomeTransactionId",
                        column: x => x.CreatedIncomeTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_AssetId",
                table: "SharedEventMovements",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_CardTransactionId",
                table: "SharedEventMovements",
                column: "CardTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_PayerPersonId",
                table: "SharedEventMovements",
                column: "PayerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_SharedEventId",
                table: "SharedEventMovements",
                column: "SharedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_SharedExpenseId",
                table: "SharedEventMovements",
                column: "SharedExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_TransactionClassId",
                table: "SharedEventMovements",
                column: "TransactionClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_TransactionId",
                table: "SharedEventMovements",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovements_UserId",
                table: "SharedEventMovements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovementShares_PersonId",
                table: "SharedEventMovementShares",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovementShares_SharedEventMovementId",
                table: "SharedEventMovementShares",
                column: "SharedEventMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventMovementShares_SharedExpenseSplitId",
                table: "SharedEventMovementShares",
                column: "SharedExpenseSplitId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventParticipants_PersonId",
                table: "SharedEventParticipants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventParticipants_SharedEventId_PersonId",
                table: "SharedEventParticipants",
                columns: new[] { "SharedEventId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_CreatedExchangeInTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "CreatedExchangeInTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_CreatedExchangeOutTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "CreatedExchangeOutTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_CreatedExpenseTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "CreatedExpenseTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_CreatedIncomeTransactionId",
                table: "SharedEventPaymentAllocations",
                column: "CreatedIncomeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_SharedEventMovementShareId",
                table: "SharedEventPaymentAllocations",
                column: "SharedEventMovementShareId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_SharedEventPaymentId",
                table: "SharedEventPaymentAllocations",
                column: "SharedEventPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPaymentAllocations_SharedExpenseSplitId",
                table: "SharedEventPaymentAllocations",
                column: "SharedExpenseSplitId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_AccountId",
                table: "SharedEventPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_AssetId",
                table: "SharedEventPayments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_FromPersonId",
                table: "SharedEventPayments",
                column: "FromPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_SharedEventId",
                table: "SharedEventPayments",
                column: "SharedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_ToPersonId",
                table: "SharedEventPayments",
                column: "ToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEventPayments_UserId",
                table: "SharedEventPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedEvents_UserId",
                table: "SharedEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedEventParticipants");

            migrationBuilder.DropTable(
                name: "SharedEventPaymentAllocations");

            migrationBuilder.DropTable(
                name: "SharedEventMovementShares");

            migrationBuilder.DropTable(
                name: "SharedEventPayments");

            migrationBuilder.DropTable(
                name: "SharedEventMovements");

            migrationBuilder.DropTable(
                name: "SharedEvents");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3906), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3908) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3912), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3912) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3913), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3913) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3914), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3914) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3915), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3915) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3916), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3916) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3917), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3917) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3918), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3918) });
        }
    }
}
