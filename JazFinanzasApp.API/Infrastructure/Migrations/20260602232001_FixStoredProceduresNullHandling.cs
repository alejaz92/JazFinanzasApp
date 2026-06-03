using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JazFinanzasApp.API.Migrations
{
    /// <inheritdoc />
    public partial class FixStoredProceduresNullHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

    -- If no reference quote, use 1 to avoid NULL multiplication
    SET @LatestReferenceQuote = ISNULL(@LatestReferenceQuote, 1);

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1) AS CumulativeFactor
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
        SUM(twq.Amount * twq.CumulativeFactor) AS Quantity,
        ISNULL(SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ), 0) AS OriginalValue,
        ISNULL(SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ), 0) AS ActualValue
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

    -- If no reference quote, use 1 to avoid NULL multiplication
    SET @LatestReferenceQuote = ISNULL(@LatestReferenceQuote, 1);

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1) AS CumulativeFactor
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
        ISNULL(SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ), 0) AS OriginalValue,
        ISNULL(SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ), 0) AS ActualValue
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to previous version (same as GetStockStatsSplitFix)
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

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1) AS CumulativeFactor
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
        SUM(twq.Amount * twq.CumulativeFactor) AS Quantity,
        SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ) AS OriginalValue,
        SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ) AS ActualValue
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

    WITH SplitFactors AS (
        SELECT
            t.Id AS TransactionId,
            ISNULL(
                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                 FROM AssetSplitEvents se
                 WHERE se.AssetId = t.AssetId
                   AND se.Date > t.Date),
            1) AS CumulativeFactor
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
        SUM(
            CASE
                WHEN twq.QuotePrice > 0 AND twq.ReferenceQuoteOnTransactionDate IS NOT NULL
                THEN (twq.Amount / twq.QuotePrice) * twq.ReferenceQuoteOnTransactionDate
                ELSE 0
            END
        ) AS OriginalValue,
        SUM(
            CASE
                WHEN twq.LatestQuote > 0
                THEN (twq.Amount * twq.CumulativeFactor / twq.LatestQuote) * @LatestReferenceQuote
                ELSE 0
            END
        ) AS ActualValue
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
        }
    }
}
