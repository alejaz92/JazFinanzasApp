using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class portfolioadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PortfolioId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateCommerceDTO",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommerceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "CryptoStatsByDateDTO",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portfolios_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StocksGralStatsDTO",
                columns: table => new
                {
                    AssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StockStatsListDTO",
                columns: table => new
                {
                    AssetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1071), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1074) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1077), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1077) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1078), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1079) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1079), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1080) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1081), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1081) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1082), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1082) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1083), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1083) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1084), new DateTime(2025, 5, 22, 18, 54, 44, 299, DateTimeKind.Utc).AddTicks(1084) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PortfolioId",
                table: "Transactions",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId",
                table: "Portfolios",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Portfolios_PortfolioId",
                table: "Transactions",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Portfolios_PortfolioId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "CryptoStatsByDateCommerceDTO");

            migrationBuilder.DropTable(
                name: "CryptoStatsByDateDTO");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropTable(
                name: "StocksGralStatsDTO");

            migrationBuilder.DropTable(
                name: "StockStatsListDTO");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PortfolioId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "Transactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9481), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9483) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9489), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9489) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9490), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9490) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9491), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9492) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9493), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9493) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9494), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9494) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9495), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9495) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9496), new DateTime(2025, 1, 22, 18, 40, 58, 301, DateTimeKind.Utc).AddTicks(9496) });
        }
    }
}
