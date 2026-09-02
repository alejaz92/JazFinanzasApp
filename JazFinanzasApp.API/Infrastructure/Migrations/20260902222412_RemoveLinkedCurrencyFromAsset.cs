using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLinkedCurrencyFromAsset : Migration
    {
        /// <inheritdoc />
        // Da de baja el reporte "Por moneda" (Patrimonio): el campo se agregó y se cargó el
        // 2026-09-02 y se descartó el mismo día porque el reporte no terminó de convencer.
        // Se elimina "hacia adelante" y no revirtiendo la migración anterior, porque esa ya se
        // había desplegado: el proceso viejo tiene la columna mapeada y dropearla por fuera del
        // deploy deja toda consulta sobre Assets tirando 500 (pasó, y se recuperó restaurando la
        // columna). Este drop tiene que correr al arrancar el proceso nuevo, que es el que ya no
        // referencia el campo — es decir, se aplica sola en el deploy, nunca a mano antes.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Assets_LinkedCurrencyAssetId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_LinkedCurrencyAssetId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LinkedCurrencyAssetId",
                table: "Assets");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1485), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1488) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1492), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1493) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1494), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1494) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1495), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1495) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1496), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1496) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1497), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1497) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1498), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1498) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1499), new DateTime(2026, 9, 2, 22, 24, 11, 443, DateTimeKind.Utc).AddTicks(1499) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedCurrencyAssetId",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5700), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5704) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5707), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5707) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5708), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5709) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5709), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5709) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5710), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5711) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5711), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5712) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5712), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5713) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5713), new DateTime(2026, 9, 2, 22, 1, 36, 691, DateTimeKind.Utc).AddTicks(5714) });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LinkedCurrencyAssetId",
                table: "Assets",
                column: "LinkedCurrencyAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Assets_LinkedCurrencyAssetId",
                table: "Assets",
                column: "LinkedCurrencyAssetId",
                principalTable: "Assets",
                principalColumn: "Id");
        }
    }
}
