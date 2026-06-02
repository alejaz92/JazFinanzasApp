using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
    }
}
