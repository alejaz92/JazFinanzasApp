using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MerchantId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MerchantId",
                table: "CardTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Merchants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MerchantAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantId = table.Column<int>(type: "int", nullable: false),
                    NormalizedDetail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantAliases_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(698), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(701) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(704), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(705) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(706), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(706) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(707), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(707) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(708), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(708) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(709), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(709) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(710), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(711) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(711), new DateTime(2026, 8, 29, 12, 51, 13, 209, DateTimeKind.Utc).AddTicks(711) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_MerchantId",
                table: "Transactions",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_MerchantId",
                table: "CardTransactions",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAliases_MerchantId",
                table: "MerchantAliases",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_UserId",
                table: "Merchants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardTransactions_Merchants_MerchantId",
                table: "CardTransactions",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Merchants_MerchantId",
                table: "Transactions",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardTransactions_Merchants_MerchantId",
                table: "CardTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Merchants_MerchantId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "MerchantAliases");

            migrationBuilder.DropTable(
                name: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_MerchantId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_CardTransactions_MerchantId",
                table: "CardTransactions");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "CardTransactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7346), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7348) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7351), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7351) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7352), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7353) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7354), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7354) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7355), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7355) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7356), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7356) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7357), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7357) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7358), new DateTime(2026, 8, 29, 12, 42, 44, 244, DateTimeKind.Utc).AddTicks(7358) });
        }
    }
}
