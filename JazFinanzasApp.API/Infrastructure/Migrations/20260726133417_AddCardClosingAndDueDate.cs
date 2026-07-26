using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardClosingAndDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextClosingDate",
                table: "Cards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextDueDate",
                table: "Cards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1899), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1901) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1904), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1904) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1905), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1905) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1906), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1907) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1907), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1907) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1908), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1908) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1909), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1909) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1910), new DateTime(2026, 7, 26, 13, 34, 16, 773, DateTimeKind.Utc).AddTicks(1910) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextClosingDate",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "NextDueDate",
                table: "Cards");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3992), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3994) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3997), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3997) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3998), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3998) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3999), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(3999) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4000), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4000) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4001), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4001) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4002), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4002) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4003), new DateTime(2026, 7, 11, 18, 42, 25, 497, DateTimeKind.Utc).AddTicks(4003) });
        }
    }
}
