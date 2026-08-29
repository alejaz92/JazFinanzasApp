using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertReportsRedesignV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionClasses_TransactionClasses_ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropTable(
                name: "CardTransactionTags");

            migrationBuilder.DropTable(
                name: "TransactionTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_TransactionClasses_ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "Nature",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TransactionClasses");

            migrationBuilder.DropColumn(
                name: "CountsAsLiquid",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Accounts");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9213), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9215) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9218), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9219) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9219), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9220) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9220), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9221) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9221), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9222) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9222), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9223) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9223), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9224) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9224), new DateTime(2026, 8, 29, 15, 8, 49, 504, DateTimeKind.Utc).AddTicks(9225) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nature",
                table: "TransactionClasses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TransactionClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsLiquid",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                        principalColumn: "Id");
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
                        principalColumn: "Id");
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
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3914), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3917) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3920), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3921) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3921), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3922) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3922), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3923) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3924), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3924) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3925), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3925) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3926), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3926) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3926), new DateTime(2026, 8, 29, 14, 31, 1, 256, DateTimeKind.Utc).AddTicks(3927) });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionClasses_TransactionClasses_ParentId",
                table: "TransactionClasses",
                column: "ParentId",
                principalTable: "TransactionClasses",
                principalColumn: "Id");
        }
    }
}
