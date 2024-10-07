using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class Agregadotablaaccountassettypecorrecta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account_AssetTypes",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    AssetTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account_AssetTypes", x => new { x.AccountId, x.AssetTypeId });
                    table.ForeignKey(
                        name: "FK_Account_AssetTypes_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Account_AssetTypes_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2112), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2116) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2119), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2119) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2120), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2121) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2122), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2122) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2123), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2123) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2124), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2124) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2125), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2125) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2126), new DateTime(2024, 10, 7, 14, 14, 34, 950, DateTimeKind.Utc).AddTicks(2126) });

            migrationBuilder.CreateIndex(
                name: "IX_Account_AssetTypes_AssetTypeId",
                table: "Account_AssetTypes",
                column: "AssetTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Account_AssetTypes");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6763), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6766) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6770), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6770) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6771), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6771) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6772), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6773) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6773), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6774) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6775), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6775) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6776), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6776) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6777), new DateTime(2024, 10, 7, 14, 8, 11, 943, DateTimeKind.Utc).AddTicks(6777) });
        }
    }
}
