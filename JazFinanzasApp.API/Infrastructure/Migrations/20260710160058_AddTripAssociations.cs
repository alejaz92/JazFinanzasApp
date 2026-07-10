using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "CardTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripSuggestionDismissals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    CardTransactionId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSuggestionDismissals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripSuggestionDismissals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSuggestionDismissals_CardTransactions_CardTransactionId",
                        column: x => x.CardTransactionId,
                        principalTable: "CardTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSuggestionDismissals_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TripSuggestionDismissals_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3906), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3908) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3912), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3912) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3913), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3913) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3914), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3914) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3915), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3915) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3916), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3916) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3917), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3917) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3918), new DateTime(2026, 7, 10, 16, 0, 58, 241, DateTimeKind.Utc).AddTicks(3918) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TripId",
                table: "Transactions",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_TripId",
                table: "CardTransactions",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSuggestionDismissals_CardTransactionId",
                table: "TripSuggestionDismissals",
                column: "CardTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSuggestionDismissals_TransactionId",
                table: "TripSuggestionDismissals",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSuggestionDismissals_TripId",
                table: "TripSuggestionDismissals",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSuggestionDismissals_UserId",
                table: "TripSuggestionDismissals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardTransactions_Trips_TripId",
                table: "CardTransactions",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Trips_TripId",
                table: "Transactions",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardTransactions_Trips_TripId",
                table: "CardTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Trips_TripId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "TripSuggestionDismissals");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TripId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_CardTransactions_TripId",
                table: "CardTransactions");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "CardTransactions");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8814), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8816) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8820), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8820) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8821), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8821) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8822), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8822) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8823), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8823) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8824), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8824) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8825), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8825) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8826), new DateTime(2026, 7, 10, 15, 38, 27, 746, DateTimeKind.Utc).AddTicks(8826) });
        }
    }
}
