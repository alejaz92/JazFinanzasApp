using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCountsAsIncomeExpenseOnSystemClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D-3 / T3: estas cuatro categorías de sistema no representan un ingreso o egreso real
            // (ajustes de saldo, o el propio movimiento de invertir) — dejan de contar en los reportes
            // de ingresos y egresos. Afecta las filas ya existentes de cada usuario; los nuevos usuarios
            // ya se crean con el flag correcto (AuthService). Reconocimiento de solo lectura hecho antes
            // de escribir: 28 filas (7 usuarios × 4 categorías), todas IsSystem = 1.
            migrationBuilder.Sql(@"
                UPDATE TransactionClasses
                SET CountsAsIncomeExpense = 0
                WHERE IsSystem = 1
                  AND Description IN ('Ajuste Saldos Ingreso', 'Ajuste Saldos Egreso', 'Inversiones', 'Ingreso Inversiones');
            ");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2061), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2063) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2095), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2096) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2097), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2097) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2098), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2098) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2099), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2099) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2100), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2100) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2101), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2101) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2102), new DateTime(2026, 8, 30, 22, 37, 13, 900, DateTimeKind.Utc).AddTicks(2102) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE TransactionClasses
                SET CountsAsIncomeExpense = 1
                WHERE IsSystem = 1
                  AND Description IN ('Ajuste Saldos Ingreso', 'Ajuste Saldos Egreso', 'Inversiones', 'Ingreso Inversiones');
            ");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2290), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2293) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2296), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2297) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2298), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2298) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2299), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2299) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2300), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2300) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2301), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2301) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2302), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2302) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2303), new DateTime(2026, 8, 30, 22, 20, 9, 268, DateTimeKind.Utc).AddTicks(2303) });
        }
    }
}
