using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize)
        {


            var totalCount = await _context.Transactions
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentTransactions.Any(im => im.IncomeTransactionId == m.Id || im.ExpenseTransactionId == m.Id))
                .CountAsync();

            var transactions = await _context.Transactions
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentTransactions.Any(im => im.IncomeTransactionId == m.Id || im.ExpenseTransactionId == m.Id))
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.TransactionClass)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();



            return (transactions, totalCount);
        }


        // get by id including related tables
        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.TransactionClass)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // get balance by asset and user, group by account
        public async Task<IEnumerable<BalanceDTO>> GetBalanceByAssetAndUserAsync(int assetId, int userId)
        {
            var balanceByAccount = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.UserId == userId && t.AssetId == assetId)
                .GroupBy(t => t.Account.Name)
                .Select(g => new BalanceDTO
                {
                    Account = g.Key,
                    Balance = g.Sum(t => t.Amount)
                })
                .Where(g => g.Balance > 0)
                .OrderByDescending(g => g.Balance)
                .ToListAsync();

            return balanceByAccount;
        }

        public async Task<IEnumerable<TotalsBalanceDTO>> GetTotalsBalanceByUserAsync(int userId)
        {
            var pesosSQL = @"
                SELECT 
                CAST(SUM(
                    CASE WHEN A.ID = 2 THEN T.Amount
                    ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN T.Amount /AQ.Value ELSE 0 END END)
                    AS decimal(18,2))
                    *
                    (SELECT VALUE FROM AssetQuotes WHERE TYPE = 'BOLSA' AND DATE = (SELECT MAX(DATE) FROM AssetQuotes)
                    ) AS TOTAL
                FROM Transactions T
                INNER JOIN Assets A ON T.AssetId = A.Id
                LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                    AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                    AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                WHERE T.UserId = @USER
            ";

            decimal totalBalancePesos = 0;
            decimal totalBalanceDollars = 0;

            try
            {
                var resultPesos = await _context.Set<TotalBalanceResult>()
                    .FromSqlRaw(pesosSQL, new SqlParameter("@USER", userId))
                    .FirstOrDefaultAsync();

                totalBalancePesos = resultPesos?.Total ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en consulta de pesos: {ex.Message}");
                throw;
            }

            var dollarSQL = @"
                SELECT 
                CAST(SUM(
                    CASE WHEN A.ID = 2 THEN T.Amount
                    ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN T.Amount /AQ.Value ELSE 0 END END)
                    AS decimal(18,2)) AS TOTAL
                FROM Transactions T
                INNER JOIN Assets A ON T.AssetId = A.Id
                LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                    AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                    AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                WHERE T.UserId = @USER
            ";

            try
            {
                var resultDollars = await _context.Set<TotalBalanceResult>()
                    .FromSqlRaw(dollarSQL, new SqlParameter("@USER", userId))
                    .FirstOrDefaultAsync();

                totalBalanceDollars = resultDollars?.Total ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en consulta de dólares: {ex.Message}");
                throw;
            }

            var totals = new List<TotalsBalanceDTO>
            {
                new TotalsBalanceDTO
                {
                    Asset = "Pesos",
                    Balance = totalBalancePesos
                },
                new TotalsBalanceDTO
                {
                    Asset = "Dólares",
                    Balance = totalBalanceDollars
                }
            };

            return totals;
        }

        public async Task<IncExpStatsDTO> GetDollarIncExpStatsAsync(int userId, DateTime month)
        {
            var dollarClassIncomeStats = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Ingreso")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "I")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .GroupBy(t => t.TransactionClass.Description)
                .Select(g => new ClassIncomeStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => t.Amount/t.QuotePrice.Value), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToListAsync();
            
            var dollarClassExpenseStats = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Egreso")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "E")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .GroupBy(t => t.TransactionClass.Description)
                .Select(g => new ClassExpenseStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => (-t.Amount)/t.QuotePrice.Value), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToListAsync();

            var totalIncomes = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.MovementType == "I")
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Ingreso")
                .Where(t => t.UserId == userId)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(t => t.Amount / t.QuotePrice.Value) // Calculando en la base de datos
                })
                .OrderByDescending(g => g.Month)
                .Take(6)
                .ToListAsync(); // Traemos los datos

            // Luego, calculamos la cantidad y redondeamos en memoria
            var totalIncomesFinal = totalIncomes
                .Select(g => new MonthIncomeStats
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que esté ordenado
                .ToList();


            var totalExpenses = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.MovementType == "E")
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Egreso")
                .Where(t => t.UserId == userId)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(t => -t.Amount / t.QuotePrice.Value) // Calculando en la base de datos
                })
                .OrderByDescending(g => g.Month)
                .Take(6)
                .ToListAsync(); // Traemos los datos

            // Luego, calculamos la cantidad y redondeamos en memoria
            var totalExpensesFinal = totalExpenses
                .Select(g => new MonthExpenseStats
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que esté ordenado
                .ToList();

            var incExpStatsDTO = new IncExpStatsDTO
            {
                ClassIncomeStats = dollarClassIncomeStats.ToArray(),
                ClassExpenseStats = dollarClassExpenseStats.ToArray(),
                MonthIncomeStats = totalIncomesFinal.ToArray(),
                MonthExpenseStats = totalExpensesFinal.ToArray()
            };

            return incExpStatsDTO;
        }
    }
}
