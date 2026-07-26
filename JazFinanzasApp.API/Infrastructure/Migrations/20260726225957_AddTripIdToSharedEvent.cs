using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripIdToSharedEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "SharedEvents",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2876), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2879) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2882), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2883), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2884), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2884) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2885), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2885) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2886), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2886) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2887), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2887) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2888), new DateTime(2026, 7, 26, 22, 59, 56, 726, DateTimeKind.Utc).AddTicks(2888) });

            migrationBuilder.CreateIndex(
                name: "IX_SharedEvents_TripId",
                table: "SharedEvents",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedEvents_Trips_TripId",
                table: "SharedEvents",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedEvents_Trips_TripId",
                table: "SharedEvents");

            migrationBuilder.DropIndex(
                name: "IX_SharedEvents_TripId",
                table: "SharedEvents");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "SharedEvents");

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
    }
}
