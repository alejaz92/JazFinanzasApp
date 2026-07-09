using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 6 de docs/plans/activos/reemplazar-stored-procedures.md: los 4 stored procedures fueron
            // reemplazados por LINQ en TransactionRepository.cs (Fases 1-4); ya no se ejecuta ningún EXEC
            // contra ellos en el código. Se retiran para no dejar procedures huérfanos en la base.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[GetStockStats];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[GetStockGralStats];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[GetCryptoStatsByDate];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[GetCryptoStatsByDateCommerce];");

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9863), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9873) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9876), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9876) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9880), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9880) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9881), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9881) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9882), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9882) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9883), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9883) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9884), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9885) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9885), new DateTime(2026, 7, 9, 20, 54, 43, 326, DateTimeKind.Utc).AddTicks(9886) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recrea los 4 procedures tal como estaban antes de esta migración (última versión conocida:
            // GetStockStats/GetStockGralStats de 20260602232002_FixDecimalCast.cs; GetCryptoStatsByDate y
            // GetCryptoStatsByDateCommerce documentados en docs/plans/activos/reemplazar-stored-procedures.md,
            // ya que nunca estuvieron versionados en el repo).
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[GetStockStats]
    @UserId INT,
    @AssetTypeId INT,
    @Environment NVARCHAR(50),
    @ConsiderStable BIT,
    @ReferenceAssetId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LatestReferenceQuote DECIMAL(18, 6);
    SELECT TOP 1 @LatestReferenceQuote = Value
    FROM AssetQuotes
    WHERE AssetId = @ReferenceAssetId AND (Type = 'BLUE' OR Type = 'NA')
    ORDER BY [Date] DESC;

    SET @LatestReferenceQuote = ISNULL(@LatestReferenceQuote, 1);

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            CAST(ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1.0) AS DECIMAL(18,10)) AS CumulativeFactor
        FROM Transactions t
        WHERE t.UserId = @UserId
    ),
    TransactionsWithQuotes AS (
        SELECT
            t.AssetId,
            t.Amount,
            t.QuotePrice,
            t.Date AS TransactionDate,
            sf.CumulativeFactor,
            aq.Value AS LatestQuote,
            (
                SELECT TOP 1 rq.Value
                FROM AssetQuotes rq
                WHERE rq.AssetId = @ReferenceAssetId
                      AND rq.[Date] <= t.Date
                      AND (rq.Type = 'BLUE' OR rq.Type = 'NA')
                ORDER BY rq.[Date] DESC
            ) AS ReferenceQuoteOnTransactionDate
        FROM Transactions t
        INNER JOIN Assets a ON t.AssetId = a.Id
        INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
        INNER JOIN SplitFactors sf ON sf.TransactionId = t.Id
        LEFT JOIN AssetQuotes aq ON t.AssetId = aq.AssetId
                                 AND aq.[Date] = (
                                     SELECT MAX([Date])
                                     FROM AssetQuotes
                                     WHERE AssetId = t.AssetId
                                 )
        WHERE
            t.UserId = @UserId AND
            at.Environment = @Environment AND
            (@AssetTypeId = 0 OR a.AssetTypeId = @AssetTypeId) AND
            (@ConsiderStable = 1 OR (a.Symbol NOT IN ('DAI', 'USDT', 'USDC')))
    )
    SELECT
        a.Name AS AssetName,
        a.Symbol,
        CAST(SUM(twq.Amount * twq.CumulativeFactor) AS DECIMAL(18,2)) AS Quantity,
        ISNULL(CAST(SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ) AS DECIMAL(18,2)), 0) AS OriginalValue,
        ISNULL(CAST(SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ) AS DECIMAL(18,2)), 0) AS ActualValue
    FROM TransactionsWithQuotes twq
    INNER JOIN Assets a ON twq.AssetId = a.Id
    GROUP BY
        a.Name,
        a.Symbol
    HAVING
        SUM(twq.Amount * twq.CumulativeFactor) > 0
    ORDER BY
        ActualValue DESC;
END;
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[GetStockGralStats]
    @UserId INT,
    @Environment NVARCHAR(50),
    @ReferenceAssetId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LatestReferenceQuote DECIMAL(18, 6);
    SELECT TOP 1 @LatestReferenceQuote = Value
    FROM AssetQuotes
    WHERE AssetId = @ReferenceAssetId AND (Type = 'BLUE' OR Type = 'NA')
    ORDER BY [Date] DESC;

    SET @LatestReferenceQuote = ISNULL(@LatestReferenceQuote, 1);

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            CAST(ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1.0) AS DECIMAL(18,10)) AS CumulativeFactor
        FROM Transactions t
        WHERE t.UserId = @UserId
    ),
    TransactionsWithQuotes AS (
        SELECT
            t.AssetId,
            t.Amount,
            t.QuotePrice,
            t.Date AS TransactionDate,
            sf.CumulativeFactor,
            aq.Value AS LatestQuote,
            (
                SELECT TOP 1 rq.Value
                FROM AssetQuotes rq
                WHERE rq.AssetId = @ReferenceAssetId
                      AND rq.[Date] <= t.Date
                      AND (rq.Type = 'BLUE' OR rq.Type = 'NA')
                ORDER BY rq.[Date] DESC
            ) AS ReferenceQuoteOnTransactionDate
        FROM Transactions t
        INNER JOIN Assets a ON t.AssetId = a.Id
        INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
        INNER JOIN SplitFactors sf ON sf.TransactionId = t.Id
        LEFT JOIN AssetQuotes aq ON t.AssetId = aq.AssetId
                                 AND aq.[Date] = (
                                     SELECT MAX([Date])
                                     FROM AssetQuotes
                                     WHERE AssetId = t.AssetId
                                 )
        WHERE
            t.UserId = @UserId AND
            at.Environment = @Environment
    )
    SELECT
        at.Name AS AssetType,
        ISNULL(CAST(SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ) AS DECIMAL(18,2)), 0) AS OriginalValue,
        ISNULL(CAST(SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ) AS DECIMAL(18,2)), 0) AS ActualValue
    FROM TransactionsWithQuotes twq
    INNER JOIN Assets a ON twq.AssetId = a.Id
    INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
    GROUP BY
        at.Name
    HAVING
        SUM(twq.Amount * twq.CumulativeFactor) > 0
    ORDER BY
        ActualValue DESC;
END;
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[GetCryptoStatsByDate]
    @UserId INT,
    @AssetTypeId INT,
    @Environment NVARCHAR(50),
    @AssetId INT = 0,
    @ConsiderStable BIT = 1,
    @ReferenceAssetId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE, @EndDate DATE;

    SELECT
        @StartDate = MIN(CAST(Date AS DATE)),
        @EndDate = MAX(CAST(Date AS DATE))
    FROM Transactions t
    INNER JOIN Assets a ON t.AssetId = a.Id
    INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
    WHERE t.UserId = @UserId
      AND at.Id = @AssetTypeId
      AND at.Environment = @Environment
      AND (@AssetId = 0 OR t.AssetId = @AssetId)
      AND (@ConsiderStable = 1 OR (a.Symbol NOT IN ('DAI', 'USDT','USDC')));

    IF @StartDate IS NULL OR @EndDate IS NULL
    BEGIN
        SELECT 'No data available for the specified criteria.' AS Message;
        RETURN;
    END;

    ;WITH DateRange AS
    (
        SELECT @StartDate AS Date
        UNION ALL
        SELECT DATEADD(DAY, 1, Date)
        FROM DateRange
        WHERE Date < @EndDate
    )

    , AccumulatedTransactions AS
    (
        SELECT
            d.Date,
            t.AssetId,
            SUM(t.Amount) AS AccumulatedAmount
        FROM DateRange d
        LEFT JOIN Transactions t
            ON CAST(t.Date AS DATE) <= d.Date
        INNER JOIN Assets a ON t.AssetId = a.Id
        INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
        WHERE t.UserId = @UserId
          AND at.Id = @AssetTypeId
          AND at.Environment = @Environment
          AND (@AssetId = 0 OR t.AssetId = @AssetId)
          AND (@ConsiderStable = 1 OR (a.Symbol NOT IN ('DAI', 'USDT', 'USDC')))
        GROUP BY d.Date, t.AssetId
    )

    , AssetQuotesWithReference AS
    (
        SELECT
            CAST(q.Date AS DATE) AS Date,
            q.AssetId,
            q.Value AS AssetValue,
            r.Value AS ReferenceValue
        FROM AssetQuotes q
        INNER JOIN Assets a ON q.AssetId = a.Id
        INNER JOIN AssetTypes at ON a.AssetTypeId = at.Id
        LEFT JOIN AssetQuotes r
            ON r.AssetId = @ReferenceAssetId
           AND r.Date = q.Date
           AND ((r.AssetId = @ReferenceAssetId AND r.Type = 'BLUE')
                 OR r.Type = 'NA')
        WHERE at.Id = @AssetTypeId
          AND at.Environment = @Environment
          AND (@AssetId = 0 OR q.AssetId = @AssetId)
          AND (@ConsiderStable = 1 OR (a.Symbol NOT IN ('DAI', 'USDT', 'USDC')))
    )

    , DailyHoldings AS
    (
        SELECT
            at.Date,
            at.AssetId,
            at.AccumulatedAmount,
            aq.AssetValue,
            COALESCE(aq.ReferenceValue, 1) AS ReferenceValue
        FROM AccumulatedTransactions at
        INNER JOIN AssetQuotesWithReference aq
            ON at.AssetId = aq.AssetId AND at.Date = aq.Date
    )

    SELECT
        dh.Date,
        SUM(dh.AccumulatedAmount / dh.AssetValue * dh.ReferenceValue) AS Value
    FROM DailyHoldings dh
    GROUP BY dh.Date
    ORDER BY dh.Date
    OPTION (MAXRECURSION 0);
END;
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[GetCryptoStatsByDateCommerce]
    @UserId INT,
    @AssetTypeId INT,
    @Environment NVARCHAR(50),
    @AssetId INT = NULL,
    @IncludeStable BIT,
    @Months INT,
    @ReferenceId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEADD(MONTH, -(@Months - 1), DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));
    DECLARE @EndDate DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

    DECLARE @MonthsTable TABLE (MonthStart DATE);
    WITH MonthCalendar AS (
        SELECT @StartDate AS MonthStart
        UNION ALL
        SELECT DATEADD(MONTH, 1, MonthStart)
        FROM MonthCalendar
        WHERE EOMONTH(MonthStart) < @EndDate
    )
    INSERT INTO @MonthsTable (MonthStart)
    SELECT MonthStart FROM MonthCalendar OPTION (MAXRECURSION 0);

    DECLARE @BaseData TABLE (
        Date DATE,
        CommerceType NVARCHAR(50),
        Value DECIMAL(18, 6)
    );

    INSERT INTO @BaseData (Date, CommerceType, Value)
    SELECT
        CAST(DATEFROMPARTS(YEAR(T.Date), MONTH(T.Date), 1) AS DATE) AS Date,
        IT.CommerceType,
        SUM(CASE
            WHEN (IT.CommerceType = 'Trading' AND A.Symbol IN ('DAI', 'USDT', 'USDC')) THEN 0
            ELSE (T.Amount * (1.0 / T.QuotePrice)) * AQ.Value
        END) AS Value
    FROM Transactions T
    INNER JOIN InvestmentTransactions IT
        ON T.Id = IT.ExpenseTransactionId OR T.Id = IT.IncomeTransactionId
    INNER JOIN Assets A
        ON T.AssetId = A.Id
    INNER JOIN AssetTypes AT
        ON A.AssetTypeId = AT.Id AND AT.Environment = @Environment AND AT.Id = @AssetTypeId
    INNER JOIN AssetQuotes AQ
        ON AQ.AssetId = @ReferenceId
           AND AQ.TYPE IN ('BLUE', 'NA')
           AND AQ.Date = (
               SELECT MAX(Date)
               FROM AssetQuotes
               WHERE AssetId = @ReferenceId AND TYPE IN ('BLUE', 'NA') AND DATE <= T.Date
           )
    WHERE
        T.UserId = @UserId AND
        T.Date >= @StartDate AND
        T.Date < DATEADD(MONTH, 1, @EndDate) AND
        (
            @IncludeStable = 1 OR
            A.Symbol NOT IN ('DAI', 'USDT', 'USDC')
        )
        AND (@AssetId IS NULL OR A.Id = @AssetId)
    GROUP BY
        YEAR(T.Date), MONTH(T.Date), IT.CommerceType;

    SELECT
        M.MonthStart AS Date,
        BD.CommerceType,
        ISNULL(SUM(BDActual.Value), 0) AS Value
    FROM @MonthsTable M
    CROSS JOIN (SELECT DISTINCT CommerceType FROM @BaseData) BD
    LEFT JOIN @BaseData BDActual
        ON M.MonthStart = BDActual.Date AND BD.CommerceType = BDActual.CommerceType
    GROUP BY M.MonthStart, BD.CommerceType
    ORDER BY M.MonthStart ASC, BD.CommerceType;
END;
");


            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5271), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5274) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5328), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5329) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5330), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5330) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5331), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5331) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5332), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5332) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5333), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5333) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5334), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5334) });

            migrationBuilder.UpdateData(
                table: "AssetTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5335), new DateTime(2026, 6, 26, 21, 59, 55, 293, DateTimeKind.Utc).AddTicks(5335) });
        }
    }
}
