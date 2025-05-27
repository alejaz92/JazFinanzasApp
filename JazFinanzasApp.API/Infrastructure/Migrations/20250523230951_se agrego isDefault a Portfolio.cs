using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class seagregoisDefaultaPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Portfolios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9171), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9173) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9176), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9177) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9177), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9178) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9178), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9179) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9179), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9180) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9180), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9181) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9182), new DateTime(2025, 5, 23, 23, 9, 49, 735, DateTimeKind.Utc).AddTicks(9182) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Portfolios");

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
        }
    }
}
