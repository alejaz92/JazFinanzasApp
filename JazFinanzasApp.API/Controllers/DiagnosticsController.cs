using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;

namespace JazFinanzasApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DiagnosticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var info = new Dictionary<string, object>();

            try
            {
                info["applied"] = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
                info["pending"] = (await _context.Database.GetPendingMigrationsAsync()).ToList();
            }
            catch (Exception ex)
            {
                info["migrationError"] = ex.Message;
            }

            var conn = _context.Database.GetDbConnection();
            try
            {
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // Check stored procedures
                using var cmd1 = conn.CreateCommand();
                cmd1.CommandText = "SELECT name FROM sys.procedures WHERE name IN ('GetStockStats', 'GetStockGralStats')";
                var procs = new List<string>();
                using (var r = await cmd1.ExecuteReaderAsync())
                    while (await r.ReadAsync()) procs.Add(r.GetString(0));
                info["storedProcedures"] = procs;

                // Check key tables
                using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"SELECT name FROM sys.tables WHERE name IN
                    ('AssetSplitEvents','Portfolios','StocksGralStatsDTO','StockStatsListDTO',
                     'StocksGralStatsResult','StockStatsListResult',
                     'CryptoStatsByDateCommerceDTO','CryptoStatsByDateDTO')";
                var tables = new List<string>();
                using (var r = await cmd2.ExecuteReaderAsync())
                    while (await r.ReadAsync()) tables.Add(r.GetString(0));
                info["existingTables"] = tables;

                // Check if PortfolioId column is nullable
                using var cmd3 = conn.CreateCommand();
                cmd3.CommandText = @"SELECT is_nullable FROM sys.columns
                    WHERE object_id = OBJECT_ID('Transactions') AND name = 'PortfolioId'";
                var val = await cmd3.ExecuteScalarAsync();
                info["portfolioIdNullable"] = val?.ToString();
            }
            catch (Exception ex)
            {
                info["dbError"] = ex.Message;
            }

            return Ok(info);
        }

        [HttpGet("test-balance")]
        public async Task<IActionResult> TestBalance([FromQuery] int userId = -1)
        {
            var result = new Dictionary<string, object>();
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd1 = conn.CreateCommand();
                cmd1.CommandText = @"SELECT name FROM sys.tables WHERE name IN
                    ('People','SharedExpenses','SharedExpenseSplits','AssetSplitEvents') ORDER BY name";
                var tables = new List<string>();
                using (var r = await cmd1.ExecuteReaderAsync())
                    while (await r.ReadAsync()) tables.Add(r.GetString(0));
                result["p1TablesExist"] = tables;
            }
            catch (Exception ex)
            {
                result["tableCheckError"] = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                var conn2 = _context.Database.GetDbConnection();
                if (conn2.State != ConnectionState.Open)
                    await conn2.OpenAsync();

                using var cmd2 = conn2.CreateCommand();
                cmd2.CommandText = @"
                    ;WITH SplitFactors AS (
                        SELECT t.Id AS TransactionId,
                            ISNULL(
                                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                                 FROM AssetSplitEvents se
                                 WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                            1) AS CumulativeFactor
                        FROM Transactions t WHERE t.UserId = @uid
                    )
                    SELECT COUNT(*) FROM SplitFactors";
                var p = cmd2.CreateParameter();
                p.ParameterName = "@uid";
                p.Value = userId;
                cmd2.Parameters.Add(p);
                var count = await cmd2.ExecuteScalarAsync();
                result["balanceSQLSyntax"] = "OK (rows=" + count + ")";
            }
            catch (Exception ex)
            {
                result["balanceSQLError"] = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                var efCount = await _context.AssetSplitEvents.CountAsync();
                result["efCoreAssetSplitEvents"] = "OK (count=" + efCount + ")";
            }
            catch (Exception ex)
            {
                result["efCoreModelError"] = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                var sqlTest = await _context.Database
                    .SqlQueryRaw<TotalBalanceResult>(
                        "SELECT CAST(0 AS decimal(18,2)) AS TOTAL")
                    .ToListAsync();
                result["efCoreSqlQueryRaw"] = "OK";
            }
            catch (Exception ex)
            {
                result["efCoreSqlQueryRawError"] = ex.GetType().Name + ": " + ex.Message;
            }

            if (userId > 0)
            {
                try
                {
                    var refAssets = await _context.Assets_Users
                        .Include(au => au.Asset)
                        .Where(au => au.UserId == userId && au.isReference)
                        .Select(au => new { au.Asset.Id, au.Asset.Name })
                        .ToListAsync();
                    result["referenceAssets"] = refAssets.Select(a => $"Id={a.Id} Name={a.Name}").ToList();
                }
                catch (Exception ex)
                {
                    result["referenceAssetsError"] = ex.GetType().Name + ": " + ex.Message;
                }

                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var pesosSQL = @"
                        ;WITH SplitFactors AS (
                            SELECT t.Id AS TransactionId,
                                ISNULL(
                                    (SELECT EXP(SUM(LOG(se.SplitRatio)))
                                     FROM AssetSplitEvents se
                                     WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                                1) AS CumulativeFactor
                            FROM Transactions t WHERE t.UserId = {0}
                        )
                        SELECT
                        CAST(SUM(
                            CASE WHEN A.ID = 2 THEN T.Amount * sf.CumulativeFactor
                            ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN (T.Amount * sf.CumulativeFactor) / AQ.Value ELSE 0 END END)
                            AS decimal(18,2))
                            *
                            (SELECT VALUE FROM AssetQuotes WHERE TYPE = 'BOLSA' AND DATE = (SELECT MAX(DATE) FROM AssetQuotes)
                            ) AS TOTAL
                        FROM Transactions T
                        INNER JOIN Assets A ON T.AssetId = A.Id
                        LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                            AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                            AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                        INNER JOIN SplitFactors sf ON sf.TransactionId = T.Id
                        WHERE T.UserId = {0}";

                    var resPesos = await _context.Database
                        .SqlQueryRaw<TotalBalanceResult>(pesosSQL, userId)
                        .ToListAsync();
                    sw.Stop();
                    result["pesosBalanceFullQuery"] = $"OK ({sw.ElapsedMilliseconds}ms, total={resPesos.FirstOrDefault()?.Total?.ToString() ?? "null"})";
                }
                catch (Exception ex)
                {
                    result["pesosBalanceFullQueryError"] = ex.GetType().Name + ": " + ex.Message;
                }

                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var dollarSQL = @"
                        ;WITH SplitFactors AS (
                            SELECT t.Id AS TransactionId,
                                ISNULL(
                                    (SELECT EXP(SUM(LOG(se.SplitRatio)))
                                     FROM AssetSplitEvents se
                                     WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                                1) AS CumulativeFactor
                            FROM Transactions t WHERE t.UserId = {0}
                        )
                        SELECT
                        CAST(SUM(
                            CASE WHEN A.ID = 2 THEN T.Amount * sf.CumulativeFactor
                            ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN (T.Amount * sf.CumulativeFactor) / AQ.Value ELSE 0 END END)
                            AS decimal(18,2)) AS TOTAL
                        FROM Transactions T
                        INNER JOIN Assets A ON T.AssetId = A.Id
                        LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                            AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                            AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                        INNER JOIN SplitFactors sf ON sf.TransactionId = T.Id
                        WHERE T.UserId = {0}";

                    var res = await _context.Database
                        .SqlQueryRaw<TotalBalanceResult>(dollarSQL, userId)
                        .ToListAsync();
                    sw.Stop();
                    result["dollarBalanceFullQuery"] = $"OK ({sw.ElapsedMilliseconds}ms, total={res.FirstOrDefault()?.Total?.ToString() ?? "null"})";
                }
                catch (Exception ex)
                {
                    result["dollarBalanceFullQueryError"] = ex.GetType().Name + ": " + ex.Message;
                }
            }

            return Ok(result);
        }
    }
}
