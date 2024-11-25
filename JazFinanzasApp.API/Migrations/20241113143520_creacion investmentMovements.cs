using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class creacioninvestmentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommerceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpenseTransactionId = table.Column<int>(type: "int", nullable: true),
                    IncomeTransactionId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_Transactions_ExpenseTransactionId",
                        column: x => x.ExpenseTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_Transactions_IncomeTransactionId",
                        column: x => x.IncomeTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_ExpenseTransactionId",
                table: "InvestmentTransactions",
                column: "ExpenseTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_IncomeTransactionId",
                table: "InvestmentTransactions",
                column: "IncomeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_UserId",
                table: "InvestmentTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentTransactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4027), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4033) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4038), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4039) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4040) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4041), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4042) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4043) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4044) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4045) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4046), new DateTime(2024, 10, 25, 17, 8, 35, 142, DateTimeKind.Utc).AddTicks(4047) });
        }
    }
}
