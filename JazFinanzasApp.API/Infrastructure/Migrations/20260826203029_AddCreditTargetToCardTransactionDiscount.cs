using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTargetToCardTransactionDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountMaterialized",
                table: "CardTransactionDiscounts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreditDate",
                table: "CardTransactionDiscounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreditTarget",
                table: "CardTransactionDiscounts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Backfill de las filas existentes: todas son de la modalidad "acreditado en cuenta",
            // y nacieron materializadas al 100% (el FIFO creaba sus Transaction de ingreso al crearse).
            // CreditDate sale de la primera cuota del descuento; si ya no le queda ninguna
            // (descuento completamente consumido), se cae al CreatedAt de la fila.
            migrationBuilder.Sql(@"
                UPDATE d
                SET d.CreditTarget = 'ACCOUNT',
                    d.AmountMaterialized = d.Amount,
                    d.CreditDate = COALESCE(
                        (SELECT MIN(i.Date)
                         FROM CardTransactionDiscountInstallments i
                         WHERE i.CardTransactionDiscountId = d.Id),
                        d.CreatedAt)
                FROM CardTransactionDiscounts d;");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountMaterialized",
                table: "CardTransactionDiscounts");

            migrationBuilder.DropColumn(
                name: "CreditDate",
                table: "CardTransactionDiscounts");

            migrationBuilder.DropColumn(
                name: "CreditTarget",
                table: "CardTransactionDiscounts");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7821), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7823) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7826), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7826) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7827), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7827) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7828), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7828) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7829), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7829) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7830), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7830) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7831), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7831) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7832), new DateTime(2026, 8, 7, 22, 11, 53, 32, DateTimeKind.Utc).AddTicks(7832) });
        }
    }
}
