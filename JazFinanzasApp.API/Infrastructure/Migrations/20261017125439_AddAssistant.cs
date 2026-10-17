using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantQualityReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PromptVersionBefore = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PromptVersionAfter = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantQualityReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantQualityReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssistantReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CutoffDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HighlightsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UnverifiedFiguresJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssistantUserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyBudgetUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantUserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantUserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssistantApiCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    CacheReadTokens = table.Column<long>(type: "bigint", nullable: false),
                    CacheWriteTokens = table.Column<long>(type: "bigint", nullable: false),
                    CostUsd = table.Column<decimal>(type: "decimal(12,6)", nullable: false),
                    StopReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    ReviewId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantApiCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantApiCalls_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssistantApiCalls_AssistantReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "AssistantReviews",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssistantFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    HasWrongFigures = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantFeedbacks_AssistantReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "AssistantReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssistantFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TransactionClassId = table.Column<int>(type: "int", nullable: true),
                    Fingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnverifiedFiguresJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantFindings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssistantFindings_AssistantReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "AssistantReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssistantFindings_TransactionClasses_TransactionClassId",
                        column: x => x.TransactionClassId,
                        principalTable: "TransactionClasses",
                        principalColumn: "Id");
                });









            migrationBuilder.CreateIndex(
                name: "IX_AssistantApiCalls_ReviewId",
                table: "AssistantApiCalls",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantApiCalls_UserId_CreatedAt",
                table: "AssistantApiCalls",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFeedbacks_ReviewId",
                table: "AssistantFeedbacks",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFindings_ReviewId",
                table: "AssistantFindings",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFindings_TransactionClassId",
                table: "AssistantFindings",
                column: "TransactionClassId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFindings_UserId_Fingerprint",
                table: "AssistantFindings",
                columns: new[] { "UserId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFindings_UserId_Status",
                table: "AssistantFindings",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantQualityReviews_UserId_ReviewedAt",
                table: "AssistantQualityReviews",
                columns: new[] { "UserId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantReviews_UserId_Kind_PeriodStart_Pending",
                table: "AssistantReviews",
                columns: new[] { "UserId", "Kind", "PeriodStart" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantReviews_UserId_Purpose_Kind_PeriodStart",
                table: "AssistantReviews",
                columns: new[] { "UserId", "Purpose", "Kind", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantUserSettings_UserId",
                table: "AssistantUserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantApiCalls");

            migrationBuilder.DropTable(
                name: "AssistantFeedbacks");

            migrationBuilder.DropTable(
                name: "AssistantFindings");

            migrationBuilder.DropTable(
                name: "AssistantQualityReviews");

            migrationBuilder.DropTable(
                name: "AssistantUserSettings");

            migrationBuilder.DropTable(
                name: "AssistantReviews");








        }
    }
}
