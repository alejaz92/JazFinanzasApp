using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedCurrencyToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedCurrencyAssetId",
                table: "Assets",
                type: "int",
                nullable: true);

            // Backfill de los 113 activos del catálogo. El criterio es "a qué moneda está atado su
            // valor", no en qué moneda cotiza en pantalla: por eso un CEDEAR va al dólar (replica el
            // papel extranjero vía CCL) y un Boncer CER va al peso aunque se opere igual que un bono
            // en dólares. NULL es una respuesta válida y deliberada: el activo no sigue a ninguna
            // moneda (cripto volátil, FCI mixto). Clasificación relevada y aprobada el 2026-09-02;
            // los fondos y la ON MR39O se verificaron uno por uno contra la ficha de cada
            // administradora y el ISIN del bono.
            // AssetTypeId: 1 Moneda · 2 Criptomoneda · 3 Accion Argentina · 4 CEDEAR · 5 FCI
            //              6 Bono · 7 Accion USA · 8 Obligacion Negociable
            migrationBuilder.Sql(@"
                -- Monedas: cada una atada a sí misma (ARS, USD, EUR, GBP, UYU, BRL)
                UPDATE Assets SET LinkedCurrencyAssetId = Id WHERE AssetTypeId = 1;

                -- CEDEARs y acciones USA: siguen al dólar
                UPDATE Assets SET LinkedCurrencyAssetId = 2 WHERE AssetTypeId IN (4, 7);

                -- Acciones argentinas: cotizan en pesos, riesgo local
                UPDATE Assets SET LinkedCurrencyAssetId = 1 WHERE AssetTypeId = 3;

                -- Cripto: solo las stablecoins están atadas al dólar; el resto queda NULL a propósito
                UPDATE Assets SET LinkedCurrencyAssetId = 2 WHERE AssetTypeId = 2 AND Symbol IN ('USDT', 'USDC', 'DAI');

                -- Bonos en dólares (incluidos los dólar-linked, que siguen al dólar aunque paguen en pesos)
                UPDATE Assets SET LinkedCurrencyAssetId = 2 WHERE AssetTypeId = 6 AND Symbol IN ('AN29', 'GD35', 'AL30', 'TZV25', 'D16E6');

                -- Bonos en pesos (ajuste CER)
                UPDATE Assets SET LinkedCurrencyAssetId = 1 WHERE AssetTypeId = 6 AND Symbol IN ('TZXO5', 'TX26', 'TX28');

                -- Obligaciones negociables: las cuatro son en dólares (MR39O confirmada por ISIN USP46214AG00)
                UPDATE Assets SET LinkedCurrencyAssetId = 2 WHERE AssetTypeId = 8;

                -- FCI en pesos
                UPDATE Assets SET LinkedCurrencyAssetId = 1 WHERE AssetTypeId = 5
                    AND Symbol IN ('SBSRPEA', 'RJDRT3A', 'BULMAAA', 'SBSACAR', 'CRTAFAA', 'ALRTAFA', 'SBSGRFA', 'RJDRTAA', 'RJMULIA');

                -- FCI dólar-linked / hard dollar
                UPDATE Assets SET LinkedCurrencyAssetId = 2 WHERE AssetTypeId = 5
                    AND Symbol IN ('BCRTAFA', 'BCRTAFB', 'COMUSAB', 'SBSCAPL');

                -- SBS Balanceado (SBSBALA) queda NULL: mixto de renta fija y variable local, no sigue
                -- limpio a ninguna moneda.
            ");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8032), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8034) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8040), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8041) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8042), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8042) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8043), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8043) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8044), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8044) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8045), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8045) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8046), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8046) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8047), new DateTime(2026, 8, 30, 23, 50, 24, 781, DateTimeKind.Utc).AddTicks(8047) });
        }
    }
}
