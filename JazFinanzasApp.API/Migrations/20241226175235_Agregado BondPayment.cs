using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoBondPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BondPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Income = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmortizationPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BondPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BondPayments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TotalBalanceResult",
                columns: table => new
                {
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(694), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(697) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(700), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(700) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(701), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(701) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(702), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(702) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(703), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(703) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(704), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(705) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(705), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(706) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(706), new DateTime(2024, 12, 26, 17, 52, 34, 219, DateTimeKind.Utc).AddTicks(707) });

            migrationBuilder.CreateIndex(
                name: "IX_BondPayments_AssetId",
                table: "BondPayments",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BondPayments");

            migrationBuilder.DropTable(
                name: "TotalBalanceResult");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5428), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5431) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5435), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5436) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5437), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5438) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5439), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5439) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5440), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5440) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5441), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5443), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5443) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5444), new DateTime(2024, 11, 25, 18, 48, 37, 330, DateTimeKind.Utc).AddTicks(5444) });
        }
    }
}
