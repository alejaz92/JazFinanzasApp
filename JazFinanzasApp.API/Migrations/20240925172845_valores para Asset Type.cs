using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class valoresparaAssetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AssetTypes",
                columns: new[] { "Id", "CreatedAt", "Environment", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1067), "FIAT", "Moneda", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1071) },
                    { 2, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1075), "CRYPTO", "Criptomoneda", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1075) },
                    { 3, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1076), "BOLSA", "Accion Argentina", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1076) },
                    { 4, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1077), "BOLSA", "CEDEAR", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1078) },
                    { 5, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1079), "BOLSA", "FCI", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1079) },
                    { 6, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1080), "BOLSA", "Bono", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1080) },
                    { 7, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1081), "BOLSA", "Accion USA", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1081) },
                    { 8, new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1082), "BOLSA", "Obligacion Negociable", new DateTime(2024, 9, 25, 17, 28, 44, 909, DateTimeKind.Utc).AddTicks(1082) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
