using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Report;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.SqlServer.Server;
using System;
using static JazFinanzasApp.API.Models.DTO.Report.StocksGralStatsDTO;

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
                .Where(t => t.TransactionClass.Description != "Ingreso Inversiones")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "I")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .GroupBy(t => t.TransactionClass.Description)
                .Select(g => new ClassIncomeStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => t.Amount / t.QuotePrice.Value), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToListAsync();

            var dollarClassExpenseStats = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Egreso")
                .Where(t => t.TransactionClass.Description != "Inversiones")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "E")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .GroupBy(t => t.TransactionClass.Description)
                .Select(g => new ClassExpenseStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => (-t.Amount) / t.QuotePrice.Value), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToListAsync();

            var totalIncomes = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.MovementType == "I")
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Ingreso")
                .Where(t => t.TransactionClass.Description != "Ingreso Inversiones")
                .Where(t => t.UserId == userId)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(t => t.Amount / t.QuotePrice.Value) // Calculando en la base de datos
                })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)    
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
                .Where(t => t.TransactionClass.Description != "Inversiones")
                .Where(t => t.UserId == userId)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(t => -t.Amount / t.QuotePrice.Value) // Calculando en la base de datos
                })
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
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

        public async Task<IncExpStatsDTO> GetPesosIncExpStatsAsync(int userId, DateTime month)
        {
            // income in pesos by class

            var incomeTransactionsInPesos = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Ingreso")
                .Where(t => t.TransactionClass.Description != "Ingreso Inversiones")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "I")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .Select(t => new
                {
                    t.TransactionClass.Description,
                    PesosAmount = t.Asset.Name == "Peso Argentino" ? t.Amount : t.Amount / t.QuotePrice.Value *
                        _context.AssetQuotes
                            .Where(aq => aq.Asset.Name == "Peso Argentino" && aq.Type == "BLUE" && aq.Date <= t.Date)
                            .OrderByDescending(aq => aq.Date)
                            .Select(aq => aq.Value)
                            .FirstOrDefault()
                })
                .ToListAsync();

            var pesosClassIncomeStats = incomeTransactionsInPesos
                .GroupBy(t => t.Description)
                .Select(g => new ClassIncomeStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => t.PesosAmount), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToList();

            // expenses in pesos by class

            var expensesTransactionsInPesos = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Egreso")
                .Where(t => t.TransactionClass.Description != "Inversiones")
                .Where(t => t.UserId == userId)
                .Where(t => t.MovementType == "E")
                .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                .Select(t => new
                {
                    t.TransactionClass.Description,
                    PesosAmount = t.Asset.Name == "Peso Argentino" ? -t.Amount : -t.Amount / t.QuotePrice.Value *
                        _context.AssetQuotes
                            .Where(aq => aq.Asset.Name == "Peso Argentino" && aq.Type == "BLUE" && aq.Date <= t.Date)
                            .OrderByDescending(aq => aq.Date)
                            .Select(aq => aq.Value)
                            .FirstOrDefault()
                })
                .ToListAsync();

            var pesosClassExpenseStats = expensesTransactionsInPesos
                .GroupBy(t => t.Description)
                .Select(g => new ClassExpenseStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(t => t.PesosAmount), 2)
                })
                .OrderByDescending(g => g.Amount)
                .ToList();





            var assetQuotes = await _context.AssetQuotes
                .Where(aq => aq.Asset.Name == "Peso Argentino" && aq.Type == "BLUE")
                .OrderByDescending(aq => aq.Date)
                .ToListAsync(); 

            // total incomes in pesos by month

            // Paso 1: Obtener transacciones relevantes desde la base de datos
            var transactionsIncome = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Include(t => t.Asset)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.MovementType == "I")
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Ingreso")
                .Where (t => t.TransactionClass.Description != "Ingreso Inversiones")
                .Where(t => t.UserId == userId)
                .ToListAsync(); // Traemos los datos a memoria

            // Paso 2: Procesar los datos en memoria


            var totalIncomes = transactionsIncome
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g =>
                {
                    var year = g.Key.Year;
                    var month = g.Key.Month;
                    var amountInPesos = g.Sum(t =>
                    {

                        if (t.Asset.Name == "Peso Argentino")
                        {
                            return t.Amount;
                        }
                        else
                        {
                            var quote = assetQuotes
                                .FirstOrDefault(aq => aq.Date <= t.Date)?.Value ?? 1; // Cotización más reciente
                            return t.Amount / (t.QuotePrice ?? 1) * quote; // Calcular en pesos
                        }

                    });

                    return new
                    {
                        Year = year,
                        Month = month,
                        Amount = amountInPesos
                    };
                })
                .OrderByDescending(g => new DateTime(g.Year, g.Month, 1)) // Ordenamos por DateTime generado
                .Take(6)
                .ToList();

            // Paso 3: Ajustar y redondear resultados
            var totalIncomesFinal = totalIncomes
                .Select(g => new MonthIncomeStats
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que esté ordenado
                .ToList();



            // total expenses in pesos by month

            // Paso 1: Obtener transacciones relevantes desde la base de datos
            var transactionsExpenses = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Include(t => t.Asset)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.MovementType == "E")
                .Where(t => t.TransactionClass.Description != "Ajuste Saldos Egreso")
                .Where(t => t.TransactionClass.Description != "Inversiones")
                .Where(t => t.UserId == userId)
                .ToListAsync(); // Traemos los datos a memoria


            // Paso 2: Procesar los datos en memoria


            var totalExpenses = transactionsExpenses
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g =>
                {
                    var year = g.Key.Year;
                    var month = g.Key.Month;
                    var amountInPesos = g.Sum(t =>
                    {
                        if (t.Asset.Name == "Peso Argentino")
                        {
                            return t.Amount;
                        }
                        else
                        {
                            var quote = assetQuotes
                                .FirstOrDefault(aq => aq.Date <= t.Date)?.Value ?? 1; // Cotización más reciente
                            return t.Amount / (t.QuotePrice ?? 1) * quote; // Calcular en pesos
                        }
                    });

                    return new
                    {
                        Year = year,
                        Month = month,
                        Amount = amountInPesos
                    };
                })
                .OrderByDescending(g => new DateTime(g.Year, g.Month, 1)) // Ordenamos por DateTime generado
                .Take(6)
                .ToList();

                // Paso 3: Ajustar y redondear resultados
                var totalExpenesesFinal = totalExpenses
                    .Select(g => new MonthExpenseStats
                    {
                        Month = new DateTime(g.Year, g.Month, 1),
                        Amount = - Math.Round(g.Amount, 2)
                    })
                    .OrderBy(g => g.Month) // Aseguramos que esté ordenado
                    .ToList();






            // Devolvemos los resultados
            var incExpStatsDTO = new IncExpStatsDTO
            {
                ClassIncomeStats = pesosClassIncomeStats.ToArray(),
                ClassExpenseStats = pesosClassExpenseStats.ToArray(),
                MonthIncomeStats = totalIncomesFinal.ToArray(),
                MonthExpenseStats = totalExpenesesFinal.ToArray()
            };

            return incExpStatsDTO;

        }

        public async Task<IEnumerable<StockStatsListDTO>> GetStockStatsAsync(int userId, int assetTypeId, string environment, bool considerStable)
        {
            var query = from transaction in _context.Transactions
                        join asset in _context.Assets on transaction.AssetId equals asset.Id
                        join assetType in _context.AssetTypes on asset.AssetTypeId equals assetType.Id
                        join latestQuote in _context.AssetQuotes on transaction.AssetId equals latestQuote.AssetId
                        where transaction.UserId == userId
                              && assetType.Environment == environment
                              && (assetTypeId == 0 || asset.AssetTypeId == assetTypeId)
                              && (considerStable == true || (asset.Symbol != "DAI" && asset.Symbol != "USDT"))
                              && latestQuote.Date == _context.AssetQuotes
                                  .Where(q => q.AssetId == transaction.AssetId)
                                  .Max(q => q.Date)
                        group new { transaction, latestQuote } by new
                        {
                            AssetName = asset.Name,
                            Symbol = asset.Symbol
                        } into grouped
                        select new StockStatsListDTO
                        {
                            AssetName = grouped.Key.AssetName,
                            Symbol = grouped.Key.Symbol,
                            Quantity = grouped.Sum(x => x.transaction.Amount),
                            OriginalValue = grouped.Sum(x => x.transaction.QuotePrice > 0
                                                                ? x.transaction.Amount / x.transaction.QuotePrice.Value
                                                                : 0),
                            ActualValue = grouped.Sum(x => x.latestQuote.Value > 0
                                                                ? x.transaction.Amount / x.latestQuote.Value
                                                                : 0)
                        };

            return await query
                .Where(dto => dto.Quantity > 0)
                .OrderByDescending(dto => dto.ActualValue).ToListAsync();
        }


        public async Task<IEnumerable<StocksGralStatsDTO>> GetStocksGralStatsAsync(int userId, string environment)
        {

           var query = from transaction in _context.Transactions
                        join asset in _context.Assets on transaction.AssetId equals asset.Id
                        join assetType in _context.AssetTypes on asset.AssetTypeId equals assetType.Id
                        join latestQuote in _context.AssetQuotes on transaction.AssetId equals latestQuote.AssetId
                        where transaction.UserId == userId
                              && assetType.Environment == environment
                              && latestQuote.Date == _context.AssetQuotes
                                  .Where(q => q.AssetId == transaction.AssetId)
                                  .Max(q => q.Date)
                        group new { transaction, latestQuote } by new
                        {
                            AssetType = assetType.Name
                        } into grouped
                        select new StocksGralStatsDTO
                        {
                            AssetType = grouped.Key.AssetType,
                            OriginalValue = grouped.Sum(x => x.transaction.QuotePrice.HasValue && x.transaction.QuotePrice > 0
                                    ? x.transaction.Amount / x.transaction.QuotePrice.Value
                                    : 0),
                            ActualValue = grouped.Sum(x => x.latestQuote.Value > 0
                                                                ? x.transaction.Amount / x.latestQuote.Value
                                                                : 0)
                        };

            return await query.OrderByDescending(dto => dto.ActualValue).ToListAsync();
        }

        public async Task<IEnumerable<CryptoStatsByDateDTO>> GetCryptoStatsByDateAsync(int userId, int assetTypeId, string environment, int? assetId, bool considerStable)
        {
            // Paso 1: Obtener las transacciones acumuladas por activo y fecha
            var accumulatedTransactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => t.Asset.AssetType.Id == assetTypeId &&
                            t.Asset.AssetType.Environment == environment &&
                            (assetId == 0 || t.Asset.Id == assetId) && 
                            (considerStable == true || (t.Asset.Symbol != "DAI" && t.Asset.Symbol != "USDT")))
                .GroupBy(t => new { t.AssetId, t.Date })
                .Select(g => new
                {
                    g.Key.AssetId,
                    g.Key.Date,
                    AccumulatedAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            // Materializar los datos para evitar problemas de traducción
            var accumulatedTransactionsLookup = accumulatedTransactions
                .ToLookup(at => at.AssetId);

            // Paso 2: Obtener las cotizaciones de los activos
            var assetQuotes = await _context.AssetQuotes
                .Where(q => q.Asset.AssetType.Id == assetTypeId &&
                            q.Asset.AssetType.Environment == environment &&
                            (assetId == 0 || q.Asset.Id == assetId) &&
                            (considerStable == true || (q.Asset.Symbol != "DAI" && q.Asset.Symbol != "USDT")))
                .ToListAsync();

            // Paso 3: Calcular las tenencias diarias en dólares en memoria
            var dailyHoldings = assetQuotes
                .GroupBy(q => q.Date)
                .Select(g => new CryptoStatsByDateDTO
                {
                    Date = g.Key,
                    Value = g.Sum(q =>
                    {
                        var transactionsForAsset = accumulatedTransactionsLookup[q.AssetId];
                        return transactionsForAsset
                            .Where(at => at.Date <= q.Date)
                            .Sum(at => at.AccumulatedAmount) / q.Value; // Division para valor en dólares
                    })
                })
                .ToList();

            return dailyHoldings;

        }

        public async Task<IEnumerable<CryptoStatsByDateCommerceDTO>> GetInvestmentsHoldingsStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months)
        {
            // Calcula las fechas del rango: mes actual y los últimos 5 meses
            DateTime endDate = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1); // Último segundo del día actual
            DateTime startDate = endDate.AddMonths(-(months-1)).AddDays(1 - endDate.Day); // Primer día del mes de hace x meses

            // Generar la lista de meses en el rango
            var monthsRange = Enumerable.Range(0, months )
                .Select(offset => startDate.AddMonths(offset))
                .Select(date => new { Year = date.Year, Month = date.Month })
                .ToList();

            // Consulta para obtener los movimientos agrupados por mes y tipo de comercio
            var query = from t in _context.Transactions
                        join it in _context.InvestmentTransactions on t.Id equals it.IncomeTransactionId into itIncome
                        from iti in itIncome.DefaultIfEmpty()
                        join it2 in _context.InvestmentTransactions on t.Id equals it2.ExpenseTransactionId into itExpense
                        from ite in itExpense.DefaultIfEmpty()
                        join a in _context.Assets on t.AssetId equals a.Id
                        join at in _context.AssetTypes on a.AssetTypeId equals at.Id
                        where t.UserId == userId &&
                              t.Date >= startDate &&
                              t.Date <= endDate &&
                              //(assetId == 0 || a.Id == assetId) &&
                              //(considerStable == true || (a.Symbol != "DAI" && a.Symbol != "USDT")) &&
                              at.Environment == environment &&
                              at.Id == assetTypeId &&
                              (iti != null || ite != null) &&
                              (iti == null || iti.MovementType != "EX") &&
                              (ite == null || ite.MovementType != "EX")
                        select new
                        {
                            t.Date.Year,
                            t.Date.Month,
                            CommerceType = iti != null ? iti.CommerceType : ite.CommerceType,
                            Value = t.Amount / t.QuotePrice
                        };

            // Agrupar los movimientos
            var groupedData = query.AsEnumerable()
                .GroupBy(x => new { x.Year, x.Month, x.CommerceType })
                .Select(g => new CryptoStatsByDateCommerceDTO
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    CommerceType = g.Key.CommerceType,
                    Value = (decimal)g.Sum(x => x.Value)
                })
                .ToList();

            // Combinar con los meses en el rango para incluir los meses sin movimientos
            var result = (from month in monthsRange
                          from commerceType in groupedData.Select(g => g.CommerceType).Distinct().DefaultIfEmpty()
                          join data in groupedData on new { month.Year, month.Month, CommerceType = commerceType }
                              equals new { data.Date.Year, data.Date.Month, data.CommerceType } into joined
                          from j in joined.DefaultIfEmpty()
                          select new CryptoStatsByDateCommerceDTO
                          {
                              Date = new DateTime(month.Year, month.Month, 1),
                              CommerceType = commerceType ?? "",
                              Value = j?.Value ?? 0
                          })
                          .OrderBy(r => r.Date)
                          .ToList();

            return result;
        }

        public async Task<IEnumerable<InvestmentTransactionsStatsDTO>> GetInvestmentsTransactionsStats(int userId, int assetId)
        {
            var transactions = _context.InvestmentTransactions
                .Include(it => it.IncomeTransaction)
                .Include(it => it.ExpenseTransaction)
                .Where(it => it.IncomeTransaction.UserId == userId || it.ExpenseTransaction.UserId == userId)
                .Where(it => it.IncomeTransaction.AssetId == assetId || it.ExpenseTransaction.AssetId == assetId)
                .Select(it => new InvestmentTransactionsStatsDTO
                {
                    Date = it.IncomeTransaction.Date,
                    Account = it.IncomeTransaction.Account.Name,
                    MovementType = it.MovementType,
                    CommerceType = it.CommerceType,
                    Quantity = Math.Abs(it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.Amount : it.ExpenseTransaction.Amount),
                    QuotePrice = 1/ (it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.QuotePrice.Value : it.ExpenseTransaction.QuotePrice.Value),
                    Total = Math.Abs(it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.Amount * 1/(it.IncomeTransaction.QuotePrice.Value) : it.ExpenseTransaction.Amount * 1/(it.ExpenseTransaction.QuotePrice.Value))

                });

            return await transactions.OrderByDescending(t => t.Date).ToListAsync();

        }


    }
    
}
