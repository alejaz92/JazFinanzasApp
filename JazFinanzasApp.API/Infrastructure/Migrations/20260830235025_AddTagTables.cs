using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CardTransactionTags",
                columns: table => new
                {
                    CardTransactionId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTransactionTags", x => new { x.CardTransactionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_CardTransactionTags_CardTransactions_CardTransactionId",
                        column: x => x.CardTransactionId,
                        principalTable: "CardTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardTransactionTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTags",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTags", x => new { x.TransactionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TransactionTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionTags_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactionTags_TagId",
                table: "CardTransactionTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionTags_TagId",
                table: "TransactionTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardTransactionTags");

            migrationBuilder.DropTable(
                name: "TransactionTags");

            migrationBuilder.DropTable(
                name: "Tags");

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
    }
}
