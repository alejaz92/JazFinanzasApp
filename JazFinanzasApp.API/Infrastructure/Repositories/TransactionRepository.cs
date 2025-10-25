using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.SqlServer.Server;
using System;
using static JazFinanzasApp.API.Business.DTO.Report.StocksGralStatsDTO;

namespace JazFinanzasApp.API.Infrastructure.Repositories
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
                .Include(m => m.Portfolio)
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
                .Include(m => m.Portfolio)
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

        public async Task<TotalsBalanceDTO> GetTotalsBalanceByUserAsync(int userId, Asset asset)
        {

            if (asset.Name == "Peso Argentino")
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


                try
                {
                    var resultPesos = await _context.Set<TotalBalanceResult>()
                        .FromSqlRaw(pesosSQL, new SqlParameter("@USER", userId))
                        .FirstOrDefaultAsync();

                    totalBalancePesos = resultPesos?.Total ?? 0;

                    var totals =
                        new TotalsBalanceDTO
                        {
                            Asset = asset.Name,
                            Symbol = asset.Symbol,
                            Color = asset.Color,
                            Balance = totalBalancePesos
                        };

                    return totals;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en consulta de pesos: {ex.Message}");
                    throw;
                }
            }
            else if (asset.Name == "Dolar Estadounidense")
            {
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
                decimal totalBalanceDollars = 0;

                try
                {
                    var resultDollars = await _context.Set<TotalBalanceResult>()
                        .FromSqlRaw(dollarSQL, new SqlParameter("@USER", userId))
                        .FirstOrDefaultAsync();

                    totalBalanceDollars = resultDollars?.Total ?? 0;

                    var totals =
                        new TotalsBalanceDTO
                        {
                            Asset = asset.Name,
                            Symbol = asset.Symbol,
                            Color = asset.Color,
                            Balance = totalBalanceDollars
                        };
                    return totals;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en consulta de dólares: {ex.Message}");
                    throw;
                }
            }
            else
            {
                var otherSQL = @"
                    SELECT 
                    CAST(SUM(
                        CASE WHEN A.ID = 2 THEN T.Amount
                        ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN T.Amount /AQ.Value ELSE 0 END END)
                        AS decimal(18,2))
                        *
                        (SELECT VALUE FROM AssetQuotes WHERE assetId = @ASSETID AND DATE = (SELECT MAX(DATE) FROM AssetQuotes)
                        ) AS TOTAL
                    FROM Transactions T
                    INNER JOIN Assets A ON T.AssetId = A.Id
                    LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                        AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                        AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                    WHERE T.UserId = @USER
                ";

                otherSQL = otherSQL.Replace("@ASSETID", asset.Id.ToString());

                decimal totalBalanceOther = 0;


                try
                {
                    var resultOther = await _context.Set<TotalBalanceResult>()
                        .FromSqlRaw(otherSQL, new SqlParameter("@USER", userId))
                        .FirstOrDefaultAsync();

                    totalBalanceOther = resultOther?.Total ?? 0;

                    var totals =
                        new TotalsBalanceDTO
                        {
                            Asset = asset.Name,
                            Symbol = asset.Symbol,
                            Color = asset.Color,
                            Balance = totalBalanceOther
                        };

                    return totals;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en consulta de {asset.Name}: {ex.Message}");
                    throw;
                }
            }




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
                    Amount = Math.Round(g.Sum(t => -t.Amount / t.QuotePrice.Value), 2)
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
                .Where(t => t.TransactionClass.Description != "Ingreso Inversiones")
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
                    Amount = -Math.Round(g.Amount, 2)
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

        public async Task<IncExpStatsDTO> GetIncExpStatsAsync(int userId, DateTime month, Asset asset /* asset destino a visualizar */)
        {
            // Rango del mes seleccionado
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Constantes (si las tenés como enums, mejor)
            const string MOV_INCOME = "I";
            const string MOV_EXPENSE = "E";
            const string CLASS_ADJ_IN = "Ajuste Saldos Ingreso";
            const string CLASS_INV_IN = "Ingreso Inversiones";
            const string CLASS_ADJ_EX = "Ajuste Saldos Egreso";
            const string CLASS_INV_EX = "Inversiones";
            const string ARS_NAME = "Peso Argentino";
            const string BLUE = "BLUE";

            // === 1) Precarga de series de cotizaciones a MEMORIA ===
            // Política: si el asset destino es ARS → usar ARS/BLUE;
            //           si el destino es otro asset → usar su propia serie.
            var isTargetARS = string.Equals(asset.Name, ARS_NAME, StringComparison.OrdinalIgnoreCase);

            var blueQuotes = new List<(DateTime Date, decimal Value)>();
            if (isTargetARS)
            {
                blueQuotes = await _context.AssetQuotes
                    .AsNoTracking()
                    .Where(aq => aq.Asset.Name == ARS_NAME && aq.Type == BLUE)
                    .OrderBy(aq => aq.Date) // asc para binary search de "última <= date"
                    .Select(aq => new { aq.Date, aq.Value })
                    .Select(x => (x.Date, x.Value))
                    .ToListAsync();
            }

            var targetQuotes = new List<(DateTime Date, decimal Value)>();
            if (!isTargetARS)
            {
                targetQuotes = await _context.AssetQuotes
                    .AsNoTracking()
                    .Where(aq => aq.Asset.Name == asset.Name)
                    .OrderBy(aq => aq.Date)
                    .Select(aq => new { aq.Date, aq.Value })
                    .Select(x => (x.Date, x.Value))
                    .ToListAsync();
            }

            // Helpers: obtienen la última cotización <= fecha, con fallback nunca 0.
            decimal GetBlueAt(DateTime date)
            {
                if (blueQuotes.Count == 0) return 1m;
                // binary search manual (última <= date)
                int lo = 0, hi = blueQuotes.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    if (blueQuotes[mid].Date <= date) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (best >= 0) return blueQuotes[best].Value;
                // si no hay <= date, usamos la primera disponible (evita 0)
                return blueQuotes[0].Value;
            }

            decimal GetTargetAt(DateTime date)
            {
                if (targetQuotes.Count == 0) return 1m;
                int lo = 0, hi = targetQuotes.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    if (targetQuotes[mid].Date <= date) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (best >= 0) return targetQuotes[best].Value;
                return targetQuotes[0].Value;
            }

            // Conversión genérica a asset destino
            decimal ConvertToTarget(int transactionAssetId, decimal amount, decimal? quotePrice, DateTime date)
            {
                // Si ya está en el asset destino, no convertir
                if (transactionAssetId == asset.Id) return amount;

                var srcQuote = quotePrice ?? 0m;
                if (srcQuote <= 0m) return 0m; // sin precio de origen válido → lo ignoramos

                if (isTargetARS)
                {
                    var blue = GetBlueAt(date);
                    return amount / srcQuote * blue;
                }
                else
                {
                    var tgt = GetTargetAt(date);
                    return amount / srcQuote * tgt;
                }
            }

            // === 2) Datos del MES por clase ===
            // Cargamos los campos mínimos y convertimos en memoria (evitamos FirstOrDefault()=0 en SQL)
            var incomesMonthRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= monthStart && t.Date < monthEnd &&
                            t.TransactionClass.Description != CLASS_ADJ_IN &&
                            t.TransactionClass.Description != CLASS_INV_IN)
                .Select(t => new
                {
                    ClassDesc = t.TransactionClass.Description,
                    t.AssetId,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            var expensesMonthRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= monthStart && t.Date < monthEnd &&
                            t.TransactionClass.Description != CLASS_ADJ_EX &&
                            t.TransactionClass.Description != CLASS_INV_EX)
                .Select(t => new
                {
                    ClassDesc = t.TransactionClass.Description,
                    t.AssetId,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            var classIncomeStats = incomesMonthRaw
                .GroupBy(x => x.ClassDesc)
                .Select(g => new ClassIncomeStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date)
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Para egresos devolvemos magnitud positiva (como en tus gráficos)
            var classExpenseStats = expensesMonthRaw
                .GroupBy(x => x.ClassDesc)
                .Select(g => new ClassExpenseStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        Math.Abs(ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date))
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // === 3) Series últimos 6 meses (limitamos lectura a ~18 meses para eficiencia) ===
            var cutoff = monthStart.AddMonths(-18);

            var incomesSeriesRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= cutoff &&
                            t.TransactionClass.Description != CLASS_ADJ_IN &&
                            t.TransactionClass.Description != CLASS_INV_IN)
                .Select(t => new { t.Date, t.AssetId, t.Amount, t.QuotePrice })
                .ToListAsync();

            var expensesSeriesRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= cutoff &&
                            t.TransactionClass.Description != CLASS_ADJ_EX &&
                            t.TransactionClass.Description != CLASS_INV_EX)
                .Select(t => new { t.Date, t.AssetId, t.Amount, t.QuotePrice })
                .ToListAsync();

            var monthIncomeStats = incomesSeriesRaw
                .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .Select(g => new MonthIncomeStats
                {
                    Month = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date)
                    ), 2)
                })
                .OrderByDescending(x => x.Month)
                .Take(6)
                .OrderBy(x => x.Month)
                .ToList();

            var monthExpenseStats = expensesSeriesRaw
                .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .Select(g => new MonthExpenseStats
                {
                    Month = g.Key,
                    // magnitud positiva para egresos
                    Amount = Math.Round(g.Sum(x =>
                        Math.Abs(ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date))
                    ), 2)
                })
                .OrderByDescending(x => x.Month)
                .Take(6)
                .OrderBy(x => x.Month)
                .ToList();

            // === 4) DTO final ===
            return new IncExpStatsDTO
            {
                ClassIncomeStats = classIncomeStats.ToArray(),
                ClassExpenseStats = classExpenseStats.ToArray(),
                MonthIncomeStats = monthIncomeStats.ToArray(),
                MonthExpenseStats = monthExpenseStats.ToArray()
            };
        }

        public async Task<IncExpStatsDTO> GetIncExpStatsAsync(int userId, DateTime month, Asset asset /* asset destino a visualizar */)
        {
            // Rango del mes seleccionado
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Constantes (si las tenés como enums, mejor)
            const string MOV_INCOME = "I";
            const string MOV_EXPENSE = "E";
            const string CLASS_ADJ_IN = "Ajuste Saldos Ingreso";
            const string CLASS_INV_IN = "Ingreso Inversiones";
            const string CLASS_ADJ_EX = "Ajuste Saldos Egreso";
            const string CLASS_INV_EX = "Inversiones";
            const string ARS_NAME = "Peso Argentino";
            const string BLUE = "BLUE";

            // === 1) Precarga de series de cotizaciones a MEMORIA ===
            // Política: si el asset destino es ARS → usar ARS/BLUE;
            //           si el destino es otro asset → usar su propia serie.
            var isTargetARS = string.Equals(asset.Name, ARS_NAME, StringComparison.OrdinalIgnoreCase);

            var blueQuotes = new List<(DateTime Date, decimal Value)>();
            if (isTargetARS)
            {
                blueQuotes = (await _context.AssetQuotes
                    .AsNoTracking()
                    .Where(aq => aq.Asset.Name == ARS_NAME && aq.Type == BLUE)
                    .OrderBy(aq => aq.Date)
                    .Select(aq => new { aq.Date, aq.Value })   // ✅ proyectamos a objeto anónimo
                    .ToListAsync())
                    .Select(x => (x.Date, x.Value))            // ✅ convertimos a tupla en memoria
                    .ToList();
            }

            var targetQuotes = new List<(DateTime Date, decimal Value)>();
            if (!isTargetARS)
            {
                targetQuotes = (await _context.AssetQuotes
                    .AsNoTracking()
                    .Where(aq => aq.Asset.Name == asset.Name)
                    .OrderBy(aq => aq.Date)
                    .Select(aq => new { aq.Date, aq.Value })
                    .ToListAsync())
                    .Select(x => (x.Date, x.Value))
                    .ToList();
            }

            // Helpers: obtienen la última cotización <= fecha, con fallback nunca 0.
            decimal GetBlueAt(DateTime date)
            {
                if (blueQuotes.Count == 0) return 1m;
                // binary search manual (última <= date)
                int lo = 0, hi = blueQuotes.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    if (blueQuotes[mid].Date <= date) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (best >= 0) return blueQuotes[best].Value;
                // si no hay <= date, usamos la primera disponible (evita 0)
                return blueQuotes[0].Value;
            }

            decimal GetTargetAt(DateTime date)
            {
                if (targetQuotes.Count == 0) return 1m;
                int lo = 0, hi = targetQuotes.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    if (targetQuotes[mid].Date <= date) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (best >= 0) return targetQuotes[best].Value;
                return targetQuotes[0].Value;
            }

            // Conversión genérica a asset destino
            decimal ConvertToTarget(int transactionAssetId, decimal amount, decimal? quotePrice, DateTime date)
            {
                // Si ya está en el asset destino, no convertir
                if (transactionAssetId == asset.Id) return amount;

                var srcQuote = quotePrice ?? 0m;
                if (srcQuote <= 0m) return 0m; // sin precio de origen válido → lo ignoramos

                if (isTargetARS)
                {
                    var blue = GetBlueAt(date);
                    return amount / srcQuote * blue;
                }
                else
                {
                    var tgt = GetTargetAt(date);
                    return amount / srcQuote * tgt;
                }
            }

            // === 2) Datos del MES por clase ===
            // Cargamos los campos mínimos y convertimos en memoria (evitamos FirstOrDefault()=0 en SQL)
            var incomesMonthRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= monthStart && t.Date < monthEnd &&
                            t.TransactionClass.Description != CLASS_ADJ_IN &&
                            t.TransactionClass.Description != CLASS_INV_IN)
                .Select(t => new
                {
                    ClassDesc = t.TransactionClass.Description,
                    t.AssetId,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            var expensesMonthRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= monthStart && t.Date < monthEnd &&
                            t.TransactionClass.Description != CLASS_ADJ_EX &&
                            t.TransactionClass.Description != CLASS_INV_EX)
                .Select(t => new
                {
                    ClassDesc = t.TransactionClass.Description,
                    t.AssetId,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            var classIncomeStats = incomesMonthRaw
                .GroupBy(x => x.ClassDesc)
                .Select(g => new ClassIncomeStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date)
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Para egresos devolvemos magnitud positiva (como en tus gráficos)
            var classExpenseStats = expensesMonthRaw
                .GroupBy(x => x.ClassDesc)
                .Select(g => new ClassExpenseStats
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        Math.Abs(ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date))
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // === 3) Series últimos 6 meses (limitamos lectura a ~18 meses para eficiencia) ===
            var cutoff = monthStart.AddMonths(-18);

            var incomesSeriesRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= cutoff &&
                            t.TransactionClass.Description != CLASS_ADJ_IN &&
                            t.TransactionClass.Description != CLASS_INV_IN)
                .Select(t => new { t.Date, t.AssetId, t.Amount, t.QuotePrice })
                .ToListAsync();

            var expensesSeriesRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= cutoff &&
                            t.TransactionClass.Description != CLASS_ADJ_EX &&
                            t.TransactionClass.Description != CLASS_INV_EX)
                .Select(t => new { t.Date, t.AssetId, t.Amount, t.QuotePrice })
                .ToListAsync();

            var monthIncomeStats = incomesSeriesRaw
                .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .Select(g => new MonthIncomeStats
                {
                    Month = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date)
                    ), 2)
                })
                .OrderByDescending(x => x.Month)
                .Take(6)
                .OrderBy(x => x.Month)
                .ToList();

            var monthExpenseStats = expensesSeriesRaw
                .GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
                .Select(g => new MonthExpenseStats
                {
                    Month = g.Key,
                    // magnitud positiva para egresos
                    Amount = Math.Round(g.Sum(x =>
                        Math.Abs(ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date))
                    ), 2)
                })
                .OrderByDescending(x => x.Month)
                .Take(6)
                .OrderBy(x => x.Month)
                .ToList();

            // === 4) DTO final ===
            return new IncExpStatsDTO
            {
                ClassIncomeStats = classIncomeStats.ToArray(),
                ClassExpenseStats = classExpenseStats.ToArray(),
                MonthIncomeStats = monthIncomeStats.ToArray(),
                MonthExpenseStats = monthExpenseStats.ToArray()
            };
        }

        public async Task<IEnumerable<StockStatsListDTO>> GetStockStatsAsync(
            int userId,
            int assetTypeId,
            string environment,
            bool considerStable,
            int referenceAssetId)
        {
            var userIdParam = new SqlParameter("@UserId", userId);
            var assetTypeIdParam = new SqlParameter("@AssetTypeId", assetTypeId);
            var environmentParam = new SqlParameter("@Environment", environment);
            var considerStableParam = new SqlParameter("@ConsiderStable", considerStable);
            var referenceAssetIdParam = new SqlParameter("@ReferenceAssetId", referenceAssetId);

            var result = await _context.StockStatsListDTO
                .FromSqlRaw("EXEC GetStockStats @UserId = {0}, @AssetTypeId = {1}, @Environment = {2}, @ConsiderStable = {3}, @ReferenceAssetId = {4}",
                            userId, assetTypeId, environment, considerStable, referenceAssetId)
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<StocksGralStatsDTO>> GetStocksGralStatsAsync(
            int userId,
            string environment,
            int referenceAssetId)
        {
            var userIdParam = new SqlParameter("@UserId", userId);
            var environmentParam = new SqlParameter("@Environment", environment);
            var referenceAssetIdParam = new SqlParameter("@ReferenceAssetId", referenceAssetId);

            var result = await _context.StocksGralStatsDTO
                .FromSqlRaw("EXEC GetStockGralStats @UserId = {0}, @Environment = {1}, @ReferenceAssetId = {2}",
                            userId, environment, referenceAssetId)
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<CryptoStatsByDateDTO>> GetCryptoStatsByDateAsync(
            int userId,
            int assetTypeId,
            string environment,
            int? assetId,
            bool considerStable,
            int referenceAssetId)
        {

            var userIdParam = new SqlParameter("@UserId", userId);
            var assetTypeIdParam = new SqlParameter("@AssetTypeId", assetTypeId);
            var environmentParam = new SqlParameter("@Environment", environment);
            var considerStableParam = new SqlParameter("@ConsiderStable", considerStable);
            var referenceAssetIdParam = new SqlParameter("@ReferenceAssetId", referenceAssetId);
            var assetIdParam = new SqlParameter("@AssetId", assetId.HasValue ? assetId : 0);

            var result = await _context.CryptoStatsByDateDTO
                .FromSqlRaw("EXEC GetCryptoStatsByDate @UserId = {0}, @AssetTypeId = {1}, @Environment = {2}, @ConsiderStable = {3}, @ReferenceAssetId = {4}, @AssetId = {5}",
                            userId, assetTypeId, environment, considerStable, referenceAssetId, assetId)
                .ToListAsync();

            return result;

        }

        public async Task<IEnumerable<CryptoStatsByDateCommerceDTO>> GetInvestmentsHoldingsStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months, int referenceId)
        {
            var result = await _context.CryptoStatsByDateCommerceDTO
                .FromSqlRaw("EXEC GetCryptoStatsByDateCommerce @UserId = {0}, @AssetTypeId = {1}, @Environment = {2}, @IncludeStable = {3}, @Months = {4}, @ReferenceId = {5}",
                            userId, assetTypeId, environment, considerStable, months, referenceId)
                .ToListAsync();

            return result;
            //// Calcula las fechas del rango: mes actual y los últimos 5 meses
            //DateTime endDate = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1); // Último segundo del día actual
            //DateTime startDate = endDate.AddMonths(-(months - 1)).AddDays(1 - endDate.Day); // Primer día del mes de hace x meses

            //// Generar la lista de meses en el rango
            //var monthsRange = Enumerable.Range(0, months)
            //    .Select(offset => startDate.AddMonths(offset))
            //    .Select(date => new { Year = date.Year, Month = date.Month })
            //    .ToList();

            //// Consulta para obtener los movimientos agrupados por mes y tipo de comercio
            //var query = from t in _context.Transactions
            //            join it in _context.InvestmentTransactions on t.Id equals it.IncomeTransactionId into itIncome
            //            from iti in itIncome.DefaultIfEmpty()
            //            join it2 in _context.InvestmentTransactions on t.Id equals it2.ExpenseTransactionId into itExpense
            //            from ite in itExpense.DefaultIfEmpty()
            //            join a in _context.Assets on t.AssetId equals a.Id
            //            join at in _context.AssetTypes on a.AssetTypeId equals at.Id
            //            where t.UserId == userId &&
            //                  t.Date >= startDate &&
            //                  t.Date <= endDate &&
            //                  //(assetId == 0 || a.Id == assetId) &&
            //                  //(considerStable == true || (a.Symbol != "DAI" && a.Symbol != "USDT")) &&
            //                  at.Environment == environment &&
            //                  at.Id == assetTypeId &&
            //                  (iti != null || ite != null) &&
            //                  (iti == null || iti.MovementType != "EX") &&
            //                  (ite == null || ite.MovementType != "EX")
            //            select new
            //            {
            //                t.Date.Year,
            //                t.Date.Month,
            //                CommerceType = iti != null ? iti.CommerceType : ite.CommerceType,
            //                Value = t.Amount / t.QuotePrice
            //            };

            //// Agrupar los movimientos
            //var groupedData = query.AsEnumerable()
            //    .GroupBy(x => new { x.Year, x.Month, x.CommerceType })
            //    .Select(g => new CryptoStatsByDateCommerceDTO
            //    {
            //        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
            //        CommerceType = g.Key.CommerceType,
            //        Value = (decimal)g.Sum(x => x.Value)
            //    })
            //    .ToList();

            //// Combinar con los meses en el rango para incluir los meses sin movimientos
            //var result = (from month in monthsRange
            //              from commerceType in groupedData.Select(g => g.CommerceType).Distinct().DefaultIfEmpty()
            //              join data in groupedData on new { month.Year, month.Month, CommerceType = commerceType }
            //                  equals new { data.Date.Year, data.Date.Month, data.CommerceType } into joined
            //              from j in joined.DefaultIfEmpty()
            //              select new CryptoStatsByDateCommerceDTO
            //              {
            //                  Date = new DateTime(month.Year, month.Month, 1),
            //                  CommerceType = commerceType ?? "",
            //                  Value = j?.Value ?? 0
            //              })
            //              .OrderBy(r => r.Date)
            //              .ToList();

            //return result;
        }

        public async Task<IEnumerable<InvestmentTransactionsStatsDTO>> GetInvestmentsTransactionsStats(int userId, int assetId, int referenceAssetId)
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
                    MovementType = it.IncomeTransaction.AssetId == assetId ? "I" : "E",
                    CommerceType = it.CommerceType,
                    Quantity = Math.Abs(it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.Amount : it.ExpenseTransaction.Amount),
                    QuotePrice = 1 / (it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.QuotePrice.Value : it.ExpenseTransaction.QuotePrice.Value) *
                        
                            _context.AssetQuotes
                                .Where(aq => aq.Asset.Id == referenceAssetId)
                                .Where(aq => aq.Type == "NA" || aq.Type == "BLUE")
                                .Where(aq => aq.Date <= it.IncomeTransaction.Date)
                                .OrderByDescending(aq => aq.Date)
                                .Select(aq => aq.Value)
                                .FirstOrDefault()
                        ,
                    Total = Math.Abs(it.IncomeTransaction.AssetId == assetId ? it.IncomeTransaction.Amount * 1 / it.IncomeTransaction.QuotePrice.Value : it.ExpenseTransaction.Amount * 1 / it.ExpenseTransaction.QuotePrice.Value) *
                        
                            _context.AssetQuotes
                                .Where(aq => aq.Asset.Id == referenceAssetId)
                                .Where(aq => aq.Type == "NA" || aq.Type == "BLUE")
                                .Where(aq => aq.Date <= it.IncomeTransaction.Date)
                                .OrderByDescending(aq => aq.Date)
                                .Select(aq => aq.Value)
                                .FirstOrDefault()
                        

                });

            return await transactions.OrderByDescending(t => t.Date).ToListAsync();

        }

        public async Task<decimal> GetAverageBuyValue(int userId, int assetId, int referenceAssetId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.QuotePrice.HasValue)
                .ToListAsync(); // Traer los datos a memoria antes de hacer cálculos

            var total = transactions.Sum(t =>
            {
                var referenceValue = _context.AssetQuotes
                    .Where(aq => aq.Asset.Id == referenceAssetId)
                    .Where(aq => aq.Type == "NA" || aq.Type == "BLUE")
                    .Where(aq => aq.Date <= t.Date)
                    .OrderByDescending(aq => aq.Date)
                    .Select(aq => aq.Value)
                    .FirstOrDefault();

                return t.Amount / t.QuotePrice.Value * referenceValue;
            });

            return total;
        }

        // get the balance for the account, asset and portfolio combination
        public async Task<decimal> GetBalance(int accountId, int assetId, int portfolioId)
        {
            var balance = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.PortfolioId == portfolioId)
                .SumAsync(t => t.Amount);
            return balance;
        }


        // get average buy value for the asset in the account and portfolio combination
        public async Task<decimal> GetAverageQuotePrice(int accountId, int assetId, int portfolioId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.PortfolioId == portfolioId)
                .Where(t => t.QuotePrice.HasValue)
                .ToListAsync(); // Traer los datos a memoria antes de hacer cálculos
            if (transactions.Count == 0) return 0;
            var total = transactions.Average(t => t.QuotePrice.Value);

            return total;
        }
    }
}
