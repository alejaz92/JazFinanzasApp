using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedExpensesP1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedExpenses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedExpenses_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedExpenseSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedExpenseId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountReimbursed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedExpenseSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedExpenseSplits_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedExpenseSplits_SharedExpenses_SharedExpenseId",
                        column: x => x.SharedExpenseId,
                        principalTable: "SharedExpenses",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1192), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1194) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1197), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1198) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1198), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1199) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1200), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1201) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1201), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1202) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1202), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1203) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1203), new DateTime(2026, 6, 15, 19, 56, 13, 177, DateTimeKind.Utc).AddTicks(1204) });

            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_TransactionId",
                table: "SharedExpenses",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenses_UserId",
                table: "SharedExpenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenseSplits_PersonId",
                table: "SharedExpenseSplits",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedExpenseSplits_SharedExpenseId",
                table: "SharedExpenseSplits",
                column: "SharedExpenseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedExpenseSplits");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "SharedExpenses");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1133), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1135) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1137), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1138) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1138), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1139) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1139), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1140) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1140), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1141) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1141), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1142) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1142), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1143) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1143), new DateTime(2026, 6, 2, 22, 15, 4, 185, DateTimeKind.Utc).AddTicks(1144) });
        }
    }
}
