using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IEnumerable<BalanceResult>> GetBalanceByAssetAndUserAsync(int assetId, int userId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.UserId == userId && t.AssetId == assetId)
                .Select(t => new { t.Account.Name, t.Amount, t.Date })
                .ToListAsync();

            var splits = await _context.AssetSplitEvents
                .Where(s => s.AssetId == assetId)
                .Select(s => new { s.Date, s.SplitRatio })
                .ToListAsync();

            var balanceByAccount = transactions
                .GroupBy(t => t.Name)
                .Select(g => new BalanceResult
                {
                    Account = g.Key,
                    Balance = g.Sum(t =>
                    {
                        var factor = splits
                            .Where(s => s.Date > t.Date)
                            .Aggregate(1m, (acc, s) => acc * s.SplitRatio);
                        return t.Amount * factor;
                    })
                })
                .Where(g => g.Balance > 0)
                .OrderByDescending(g => g.Balance)
                .ToList();

            return balanceByAccount;
        }

        public async Task<TotalsBalanceResult> GetTotalsBalanceByUserAsync(int userId, Asset asset)
        {

            if (asset.Name == "Peso Argentino")
            {
                var pesosSQL = @"
                    ;WITH SplitFactors AS (
                        SELECT t.Id AS TransactionId,
                            ISNULL(
                                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                                 FROM AssetSplitEvents se
                                 WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                            1) AS CumulativeFactor
                        FROM Transactions t WHERE t.UserId = @USER
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
                    WHERE T.UserId = @USER
                    OPTION(RECOMPILE)
                ";

                decimal totalBalancePesos = 0;


                try
                {
                    var resultPesos = (await _context.Database
                        .SqlQueryRaw<TotalBalanceResult>(pesosSQL, new SqlParameter("@USER", userId))
                        .ToListAsync()).FirstOrDefault();

                    totalBalancePesos = resultPesos?.Total ?? 0;

                    var totals =
                        new TotalsBalanceResult
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
                ;WITH SplitFactors AS (
                    SELECT t.Id AS TransactionId,
                        ISNULL(
                            (SELECT EXP(SUM(LOG(se.SplitRatio)))
                             FROM AssetSplitEvents se
                             WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                        1) AS CumulativeFactor
                    FROM Transactions t WHERE t.UserId = @USER
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
                WHERE T.UserId = @USER
                OPTION(RECOMPILE)
                ";
                decimal totalBalanceDollars = 0;

                try
                {
                    var resultDollars = (await _context.Database
                        .SqlQueryRaw<TotalBalanceResult>(dollarSQL, new SqlParameter("@USER", userId))
                        .ToListAsync()).FirstOrDefault();

                    totalBalanceDollars = resultDollars?.Total ?? 0;

                    var totals =
                        new TotalsBalanceResult
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
                    Console.WriteLine($"Error en consulta de d?lares: {ex.Message}");
                    throw;
                }
            }
            else
            {
                var otherSQL = @"
                    ;WITH SplitFactors AS (
                        SELECT t.Id AS TransactionId,
                            ISNULL(
                                (SELECT EXP(SUM(LOG(se.SplitRatio)))
                                 FROM AssetSplitEvents se
                                 WHERE se.AssetId = t.AssetId AND se.Date > t.Date),
                            1) AS CumulativeFactor
                        FROM Transactions t WHERE t.UserId = @USER
                    )
                    SELECT
                    CAST(SUM(
                        CASE WHEN A.ID = 2 THEN T.Amount * sf.CumulativeFactor
                        ELSE CASE WHEN AQ.Value IS NOT NULL AND AQ.Value <> 0 THEN (T.Amount * sf.CumulativeFactor) / AQ.Value ELSE 0 END END)
                        AS decimal(18,2))
                        *
                        (SELECT VALUE FROM AssetQuotes WHERE assetId = @ASSETID AND DATE = (SELECT MAX(DATE) FROM AssetQuotes)
                        ) AS TOTAL
                    FROM Transactions T
                    INNER JOIN Assets A ON T.AssetId = A.Id
                    LEFT JOIN AssetQuotes AQ ON AQ.AssetId = A.Id
                        AND AQ.Date = (SELECT MAX(Date) FROM AssetQuotes)
                        AND AQ.Type <> 'TARJETA' AND AQ.Type <> 'BLUE'
                    INNER JOIN SplitFactors sf ON sf.TransactionId = T.Id
                    WHERE T.UserId = @USER
                    OPTION(RECOMPILE)
                ";

                decimal totalBalanceOther = 0;


                try
                {
                    var resultOther = (await _context.Database
                        .SqlQueryRaw<TotalBalanceResult>(otherSQL, new SqlParameter("@ASSETID", asset.Id), new SqlParameter("@USER", userId))
                        .ToListAsync()).FirstOrDefault();

                    totalBalanceOther = resultOther?.Total ?? 0;

                    var totals =
                        new TotalsBalanceResult
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

        public async Task<IncExpResult> GetDollarIncExpStatsAsync(int userId, DateTime month)
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
                .Select(g => new ClassIncomeResult
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
                .Select(g => new ClassExpenseResult
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
                .Select(g => new MonthIncomeResult
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que est? ordenado
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
                .Select(g => new MonthExpenseResult
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que est? ordenado
                .ToList();

            var incExpStatsDTO = new IncExpResult
            {
                ClassIncomeStats = dollarClassIncomeStats.ToArray(),
                ClassExpenseStats = dollarClassExpenseStats.ToArray(),
                MonthIncomeStats = totalIncomesFinal.ToArray(),
                MonthExpenseStats = totalExpensesFinal.ToArray()
            };


            return incExpStatsDTO;
        }

        public async Task<IncExpResult> GetPesosIncExpStatsAsync(int userId, DateTime month)
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
                .Select(g => new ClassIncomeResult
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
                .Select(g => new ClassExpenseResult
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
                                .FirstOrDefault(aq => aq.Date <= t.Date)?.Value ?? 1; // Cotizaci?n m?s reciente
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
                .Select(g => new MonthIncomeResult
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que est? ordenado
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
                                .FirstOrDefault(aq => aq.Date <= t.Date)?.Value ?? 1; // Cotizaci?n m?s reciente
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
                .Select(g => new MonthExpenseResult
                {
                    Month = new DateTime(g.Year, g.Month, 1),
                    Amount = -Math.Round(g.Amount, 2)
                })
                .OrderBy(g => g.Month) // Aseguramos que est? ordenado
                .ToList();






            // Devolvemos los resultados
            var incExpStatsDTO = new IncExpResult
            {
                ClassIncomeStats = pesosClassIncomeStats.ToArray(),
                ClassExpenseStats = pesosClassExpenseStats.ToArray(),
                MonthIncomeStats = totalIncomesFinal.ToArray(),
                MonthExpenseStats = totalExpenesesFinal.ToArray()
            };

            return incExpStatsDTO;

        }


        public async Task<IncExpResult> GetIncExpStatsAsync(int userId, DateTime month, Asset asset /* asset destino a visualizar */)
        {
            // Rango del mes seleccionado
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Constantes (si las ten?s como enums, mejor)
            const string MOV_INCOME = "I";
            const string MOV_EXPENSE = "E";
            const string CLASS_ADJ_IN = "Ajuste Saldos Ingreso";
            const string CLASS_INV_IN = "Ingreso Inversiones";
            const string CLASS_ADJ_EX = "Ajuste Saldos Egreso";
            const string CLASS_INV_EX = "Inversiones";
            const string ARS_NAME = "Peso Argentino";
            const string BLUE = "BLUE";

            // === 1) Precarga de series de cotizaciones a MEMORIA ===
            // Pol?tica: si el asset destino es ARS ? usar ARS/BLUE;
            //           si el destino es otro asset ? usar su propia serie.
            var isTargetARS = string.Equals(asset.Name, ARS_NAME, StringComparison.OrdinalIgnoreCase);

            var blueQuotes = new List<(DateTime Date, decimal Value)>();
            if (isTargetARS)
            {
                blueQuotes = (await _context.AssetQuotes
                    .AsNoTracking()
                    .Where(aq => aq.Asset.Name == ARS_NAME && aq.Type == BLUE)
                    .OrderBy(aq => aq.Date)
                    .Select(aq => new { aq.Date, aq.Value })   // ? proyectamos a objeto an?nimo
                    .ToListAsync())
                    .Select(x => (x.Date, x.Value))            // ? convertimos a tupla en memoria
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

            // Helpers: obtienen la ?ltima cotizaci?n <= fecha, con fallback nunca 0.
            decimal GetBlueAt(DateTime date)
            {
                if (blueQuotes.Count == 0) return 1m;
                // binary search manual (?ltima <= date)
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

            // Conversi?n gen?rica a asset destino
            decimal ConvertToTarget(int transactionAssetId, decimal amount, decimal? quotePrice, DateTime date)
            {
                // Si ya est? en el asset destino, no convertir
                if (transactionAssetId == asset.Id) return amount;

                var srcQuote = quotePrice ?? 0m;
                if (srcQuote <= 0m) return 0m; // sin precio de origen v?lido ? lo ignoramos

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
            // Cargamos los campos m?nimos y convertimos en memoria (evitamos FirstOrDefault()=0 en SQL)
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
                .Select(g => new ClassIncomeResult
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date)
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Para egresos devolvemos magnitud positiva (como en tus gr?ficos)
            var classExpenseStats = expensesMonthRaw
                .GroupBy(x => x.ClassDesc)
                .Select(g => new ClassExpenseResult
                {
                    TransactionClass = g.Key,
                    Amount = Math.Round(g.Sum(x =>
                        Math.Abs(ConvertToTarget(x.AssetId, x.Amount, x.QuotePrice, x.Date))
                    ), 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // === 3) Series ?ltimos 6 meses (limitamos lectura a ~18 meses para eficiencia) ===
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
                .Select(g => new MonthIncomeResult
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
                .Select(g => new MonthExpenseResult
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
            return new IncExpResult
            {
                ClassIncomeStats = classIncomeStats.ToArray(),
                ClassExpenseStats = classExpenseStats.ToArray(),
                MonthIncomeStats = monthIncomeStats.ToArray(),
                MonthExpenseStats = monthExpenseStats.ToArray()
            };
        }

        // Contribución de una transacción individual a las stats de inversión: cantidad, valor original
        // (a la cotización de referencia del momento de la compra) y valor actual (a la última cotización
        // conocida). Extraído para que GetStockStatsAsync y GetStocksGralStatsAsync (Fase 1/2 de
        // docs/plans/activos/reemplazar-stored-procedures.md) compartan el cálculo en vez de duplicarlo
        // como hacían los stored procedures GetStockStats/GetStockGralStats originales.
        private class InvestmentValueContribution
        {
            public string AssetName { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string AssetTypeName { get; set; } = "";
            public decimal QuantityContribution { get; set; }
            public decimal OriginalValueContribution { get; set; }
            public decimal ActualValueContribution { get; set; }
        }

        private async Task<List<InvestmentValueContribution>> GetInvestmentValueContributionsAsync(
            int userId, string environment, int referenceAssetId, int assetTypeId, bool considerStable)
        {
            var stableSymbols = new[] { "DAI", "USDT", "USDC" };

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => t.Asset.AssetType.Environment == environment)
                .Where(t => assetTypeId == 0 || t.Asset.AssetTypeId == assetTypeId)
                .Where(t => considerStable || !stableSymbols.Contains(t.Asset.Symbol))
                .Select(t => new
                {
                    t.AssetId,
                    AssetName = t.Asset.Name,
                    t.Asset.Symbol,
                    AssetTypeName = t.Asset.AssetType.Name,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            if (transactions.Count == 0)
                return new List<InvestmentValueContribution>();

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();
            var earliestTransactionDate = transactions.Min(t => t.Date);

            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();

            // Última cotización de cada activo, sin filtrar por Type (igual que el LEFT JOIN del SP
            // original, que resuelve por MAX(Date) sin distinguir Type). En la práctica los activos de
            // Bolsa/Cripto solo tienen un Type de cotización por día, así que esto no genera fan-out;
            // se suma igual por si hubiera más de una fila para la misma fecha, para mantener la misma
            // semántica que tendría el LEFT JOIN del SP en ese caso.
            // El MAX(Date) se resuelve con una subquery correlacionada en el propio SQL Server (igual
            // que el SP original) en vez de traer todo el historial de cotizaciones a memoria.
            var latestQuoteByAsset = await _context.AssetQuotes
                .Where(q => assetIds.Contains(q.AssetId))
                .Where(q => q.Date == _context.AssetQuotes
                    .Where(q2 => q2.AssetId == q.AssetId)
                    .Max(q2 => q2.Date))
                .GroupBy(q => q.AssetId)
                .Select(g => new { AssetId = g.Key, Value = g.Sum(q => q.Value) })
                .ToDictionaryAsync(x => x.AssetId, x => x.Value);

            // Acotado a partir de la transacción más antigua: no hace falta cotización de referencia
            // anterior a eso. El límite superior queda abierto porque la más reciente puede ser
            // posterior a la última transacción.
            var referenceQuotes = await _context.AssetQuotes
                .Where(q => q.AssetId == referenceAssetId && (q.Type == "BLUE" || q.Type == "NA"))
                .Where(q => q.Date >= earliestTransactionDate)
                .OrderByDescending(q => q.Date)
                .Select(q => new { q.Date, q.Value })
                .ToListAsync();

            var latestReferenceQuote = referenceQuotes.FirstOrDefault()?.Value ?? 1m;

            decimal GetReferenceQuoteOnOrBefore(DateTime date) =>
                referenceQuotes.FirstOrDefault(q => q.Date <= date)?.Value ?? 0m;

            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            return transactions
                .Select(t =>
                {
                    var cumulativeFactor = GetSplitFactor(t.AssetId, t.Date);
                    var referenceQuoteOnDate = GetReferenceQuoteOnOrBefore(t.Date);
                    var latestQuote = latestQuoteByAsset.TryGetValue(t.AssetId, out var q) ? q : 0m;

                    return new InvestmentValueContribution
                    {
                        AssetName = t.AssetName,
                        Symbol = t.Symbol,
                        AssetTypeName = t.AssetTypeName,
                        QuantityContribution = t.Amount * cumulativeFactor,
                        OriginalValueContribution = t.QuotePrice.HasValue && t.QuotePrice.Value > 0 && referenceQuoteOnDate > 0
                            ? (t.Amount / t.QuotePrice.Value) * referenceQuoteOnDate
                            : 0m,
                        ActualValueContribution = latestQuote > 0
                            ? (t.Amount * cumulativeFactor / latestQuote) * latestReferenceQuote
                            : 0m
                    };
                })
                .ToList();
        }

        // Reemplaza al stored procedure [dbo].[GetStockStats] (ver docs/plans/activos/reemplazar-stored-procedures.md, Fase 1).
        public async Task<IEnumerable<StockStatsListResult>> GetStockStatsAsync(
            int userId,
            int assetTypeId,
            string environment,
            bool considerStable,
            int referenceAssetId)
        {
            var contributions = await GetInvestmentValueContributionsAsync(userId, environment, referenceAssetId, assetTypeId, considerStable);

            return contributions
                .GroupBy(c => new { c.AssetName, c.Symbol })
                .Select(g => new
                {
                    g.Key.AssetName,
                    g.Key.Symbol,
                    RawQuantity = g.Sum(c => c.QuantityContribution),
                    RawOriginalValue = g.Sum(c => c.OriginalValueContribution),
                    RawActualValue = g.Sum(c => c.ActualValueContribution)
                })
                .Where(x => x.RawQuantity > 0) // HAVING SUM(Amount * CumulativeFactor) > 0 en el SP original
                .Select(x => new StockStatsListResult
                {
                    AssetName = x.AssetName,
                    Symbol = x.Symbol,
                    Quantity = Math.Round(x.RawQuantity, 2),
                    OriginalValue = Math.Round(x.RawOriginalValue, 2),
                    ActualValue = Math.Round(x.RawActualValue, 2)
                })
                .OrderByDescending(r => r.ActualValue)
                .ToList();
        }

        // Reemplaza al stored procedure [dbo].[GetStockGralStats] (ver docs/plans/activos/reemplazar-stored-procedures.md, Fase 2).
        // A diferencia de GetStockStats, no filtra por AssetTypeId ni excluye stablecoins (el SP original
        // tampoco lo hacía) y agrupa por tipo de activo en vez de por activo individual.
        public async Task<IEnumerable<StocksGralStatsResult>> GetStocksGralStatsAsync(
            int userId,
            string environment,
            int referenceAssetId)
        {
            var contributions = await GetInvestmentValueContributionsAsync(userId, environment, referenceAssetId, assetTypeId: 0, considerStable: true);

            return contributions
                .GroupBy(c => c.AssetTypeName)
                .Select(g => new
                {
                    AssetType = g.Key,
                    RawQuantity = g.Sum(c => c.QuantityContribution),
                    RawOriginalValue = g.Sum(c => c.OriginalValueContribution),
                    RawActualValue = g.Sum(c => c.ActualValueContribution)
                })
                .Where(x => x.RawQuantity > 0) // HAVING SUM(Amount * CumulativeFactor) > 0 en el SP original
                .Select(x => new StocksGralStatsResult
                {
                    AssetType = x.AssetType,
                    OriginalValue = Math.Round(x.RawOriginalValue, 2),
                    ActualValue = Math.Round(x.RawActualValue, 2)
                })
                .OrderByDescending(r => r.ActualValue)
                .ToList();
        }

        // Reemplaza al stored procedure [dbo].[GetCryptoStatsByDate] (ver docs/plans/activos/reemplazar-stored-procedures.md,
        // Fase 3). No estaba versionado en el repo — se extrajo directamente de la base con "Script as CREATE".
        //
        // Replica fielmente el comportamiento del SP, incluyendo dos particularidades que no son evidentes a simple
        // vista pero que hacen a la paridad de resultados:
        // 1. La cotización de referencia se empareja por fecha EXACTA (no "la más reciente disponible" como en
        //    GetStockStats) y si no hay ninguna para ese día puntual, se usa 1 como valor por defecto (COALESCE).
        // 2. El valor final NO se redondea (a diferencia de GetStockStats, que sí castea a DECIMAL(18,2)).
        // 3. Un día solo aparece en el resultado si el activo tiene una cotización cargada para esa fecha exacta —
        //    no hay relleno con "el último precio conocido" para los días sin cotización (gaps de carga de precios).
        public async Task<IEnumerable<CryptoStatsByDateResult>> GetCryptoStatsByDateAsync(
            int userId,
            int assetTypeId,
            string environment,
            int? assetId,
            bool considerStable,
            int referenceAssetId)
        {
            var stableSymbols = new[] { "DAI", "USDT", "USDC" };
            var effectiveAssetId = assetId ?? 0;

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => t.Asset.AssetTypeId == assetTypeId)
                .Where(t => t.Asset.AssetType.Environment == environment)
                .Where(t => effectiveAssetId == 0 || t.AssetId == effectiveAssetId)
                .Where(t => considerStable || !stableSymbols.Contains(t.Asset.Symbol))
                .Select(t => new { t.AssetId, t.Amount, Date = t.Date.Date })
                .ToListAsync();

            if (transactions.Count == 0)
                return Enumerable.Empty<CryptoStatsByDateResult>();

            var startDate = transactions.Min(t => t.Date);
            var endDate = transactions.Max(t => t.Date);

            // Checkpoints ordenados por activo para poder calcular la tenencia acumulada a una fecha dada
            // (equivalente a SUM(Amount) WHERE Date <= d, pero sin recorrer todas las transacciones por día).
            var transactionsByAsset = transactions
                .GroupBy(t => t.AssetId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

            decimal? GetAccumulatedAmountAsOf(int assetId, DateTime date)
            {
                if (!transactionsByAsset.TryGetValue(assetId, out var checkpoints)) return null;
                var upToDate = checkpoints.Where(c => c.Date <= date).ToList();
                return upToDate.Count == 0 ? null : upToDate.Sum(c => c.Amount);
            }

            var relevantAssetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            // Cotizaciones de los activos (cualquier Type, igual que el SP original), acotadas al rango de
            // fechas relevante en vez de traer todo el historial.
            var assetQuotes = await _context.AssetQuotes
                .Where(q => relevantAssetIds.Contains(q.AssetId))
                .Where(q => q.Date >= startDate && q.Date <= endDate)
                .Select(q => new { q.AssetId, Date = q.Date.Date, q.Value })
                .ToListAsync();

            // Cotizaciones de referencia por fecha exacta (puede haber más de una si el activo de referencia
            // tiene, por ejemplo, tipo BLUE y NA el mismo día — se replica el fan-out que produciría el LEFT
            // JOIN del SP en ese caso).
            var referenceQuotesByDate = (await _context.AssetQuotes
                    .Where(q => q.AssetId == referenceAssetId && (q.Type == "BLUE" || q.Type == "NA"))
                    .Where(q => q.Date >= startDate && q.Date <= endDate)
                    .Select(q => new { Date = q.Date.Date, q.Value })
                    .ToListAsync())
                .GroupBy(q => q.Date)
                .ToDictionary(g => g.Key, g => g.Select(q => q.Value).ToList());

            var contributions = new List<(DateTime Date, decimal Value)>();

            foreach (var q in assetQuotes)
            {
                if (q.Value == 0) continue; // evita división por cero; el SP no la protege pero nunca debería darse en datos reales

                var accumulatedAmount = GetAccumulatedAmountAsOf(q.AssetId, q.Date);
                if (accumulatedAmount == null) continue; // sin transacciones del activo hasta esa fecha -> excluido (INNER JOIN)

                var referenceValues = referenceQuotesByDate.TryGetValue(q.Date, out var refs) ? refs : new List<decimal> { 1m };
                foreach (var referenceValue in referenceValues)
                    contributions.Add((q.Date, accumulatedAmount.Value / q.Value * referenceValue));
            }

            return contributions
                .GroupBy(c => c.Date)
                .Select(g => new CryptoStatsByDateResult { Date = g.Key, Value = g.Sum(c => c.Value) })
                .OrderBy(r => r.Date)
                .ToList();
        }

        // Reemplaza al stored procedure [dbo].[GetCryptoStatsByDateCommerce] (ver docs/plans/activos/reemplazar-stored-procedures.md,
        // Fase 4). Tampoco estaba versionado en el repo — se extrajo directamente de la base con "Script as CREATE".
        //
        // Particularidades del SP replicadas acá (distintas a las Fases 1-3):
        // 1. Solo considera Transaction que forman parte de un InvestmentTransaction (como ExpenseTransactionId o
        //    IncomeTransactionId) — no cualquier movimiento de cuenta.
        // 2. La cotización de referencia usa "la más reciente <= la fecha de la transacción" (como GetStockStats),
        //    pero acá el JOIN es INNER: si no hay ninguna cotización de referencia a esa fecha, la transacción se
        //    excluye por completo (no cae a 1 como en GetCryptoStatsByDate).
        //    (@AssetId): el parámetro nunca se enviaba al SP en la llamada existente (faltaba en el SqlQueryRaw),
        //    por lo que el filtro por activo específico jamás se aplicaba en la práctica — hoy solo se llama con
        //    assetId=0, así que el comportamiento observado no cambia, pero queda corregido para cuando se necesite.
        // 3. Si el CommerceType es exactamente "Trading" y el símbolo es una stablecoin, el valor de esa fila se
        //    fuerza a 0 aunque @IncludeStable/considerStable sea true (no se excluye la fila, se anula su aporte;
        //    con otros CommerceType, una stablecoin si se incluye sí aporta su valor real).
        // 4. El resultado final rellena con 0 todos los meses del calendario para cada CommerceType que aparezca
        //    en algún mes del rango (no solo los meses donde ese CommerceType efectivamente tuvo movimientos).
        public async Task<IEnumerable<CryptoStatsByDateCommerceResult>> GetInvestmentsHoldingsStats(int userId, int assetTypeId, string environment, int? assetId, bool considerStable, int months, int referenceId)
        {
            var stableSymbols = new[] { "DAI", "USDT", "USDC" };
            var effectiveAssetId = assetId ?? 0;

            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var startDate = currentMonthStart.AddMonths(-(months - 1));
            var upperBoundExclusive = currentMonthStart.AddMonths(1); // primer día del mes siguiente al actual

            var expenseSide = _context.InvestmentTransactions
                .Where(it => it.ExpenseTransactionId != null)
                .Select(it => new
                {
                    it.CommerceType,
                    it.ExpenseTransaction!.UserId,
                    it.ExpenseTransaction!.AssetId,
                    AssetTypeId = it.ExpenseTransaction!.Asset.AssetTypeId,
                    Environment = it.ExpenseTransaction!.Asset.AssetType.Environment,
                    Symbol = it.ExpenseTransaction!.Asset.Symbol,
                    it.ExpenseTransaction!.Date,
                    it.ExpenseTransaction!.Amount,
                    it.ExpenseTransaction!.QuotePrice
                });

            var incomeSide = _context.InvestmentTransactions
                .Where(it => it.IncomeTransactionId != null)
                .Select(it => new
                {
                    it.CommerceType,
                    it.IncomeTransaction!.UserId,
                    it.IncomeTransaction!.AssetId,
                    AssetTypeId = it.IncomeTransaction!.Asset.AssetTypeId,
                    Environment = it.IncomeTransaction!.Asset.AssetType.Environment,
                    Symbol = it.IncomeTransaction!.Asset.Symbol,
                    it.IncomeTransaction!.Date,
                    it.IncomeTransaction!.Amount,
                    it.IncomeTransaction!.QuotePrice
                });

            var rows = await expenseSide.Concat(incomeSide)
                .Where(r => r.UserId == userId)
                .Where(r => r.AssetTypeId == assetTypeId)
                .Where(r => r.Environment == environment)
                .Where(r => effectiveAssetId == 0 || r.AssetId == effectiveAssetId)
                .Where(r => considerStable || !stableSymbols.Contains(r.Symbol))
                .Where(r => r.Date >= startDate && r.Date < upperBoundExclusive)
                .ToListAsync();

            if (rows.Count == 0)
                return Enumerable.Empty<CryptoStatsByDateCommerceResult>();

            var maxRowDate = rows.Max(r => r.Date);
            var referenceQuotes = await _context.AssetQuotes
                .Where(q => q.AssetId == referenceId && (q.Type == "BLUE" || q.Type == "NA"))
                .Where(q => q.Date <= maxRowDate)
                .OrderByDescending(q => q.Date)
                .Select(q => new { q.Date, q.Value })
                .ToListAsync();

            decimal? GetReferenceQuoteOnOrBeforeOrNull(DateTime date) =>
                referenceQuotes.FirstOrDefault(q => q.Date <= date)?.Value;

            var monthlyContributions = rows
                .Select(r =>
                {
                    var referenceQuote = GetReferenceQuoteOnOrBeforeOrNull(r.Date);
                    if (referenceQuote == null) return ((DateTime Month, string CommerceType, decimal Value)?)null; // INNER JOIN: sin cotización de referencia -> se excluye
                    if (!r.QuotePrice.HasValue || r.QuotePrice.Value == 0) return null;

                    var isZeroedStableTrading = r.CommerceType == "Trading" && stableSymbols.Contains(r.Symbol);
                    var value = isZeroedStableTrading ? 0m : r.Amount * (1m / r.QuotePrice.Value) * referenceQuote.Value;

                    return (Month: new DateTime(r.Date.Year, r.Date.Month, 1), r.CommerceType, Value: value);
                })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .GroupBy(x => new { x.Month, x.CommerceType })
                .Select(g => new { g.Key.Month, g.Key.CommerceType, Value = Math.Round(g.Sum(x => x.Value), 6) }) // DECIMAL(18,6) en el SP original
                .ToList();

            var commerceTypes = monthlyContributions.Select(c => c.CommerceType).Distinct().ToList();

            var result = new List<CryptoStatsByDateCommerceResult>();
            for (var month = startDate; month <= currentMonthStart; month = month.AddMonths(1))
            {
                foreach (var commerceType in commerceTypes)
                {
                    var match = monthlyContributions.FirstOrDefault(c => c.Month == month && c.CommerceType == commerceType);
                    result.Add(new CryptoStatsByDateCommerceResult
                    {
                        Date = month,
                        CommerceType = commerceType,
                        Value = match?.Value ?? 0m
                    });
                }
            }

            return result
                .OrderBy(r => r.Date)
                .ThenBy(r => r.CommerceType, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<IEnumerable<InvestmentTransactionsResult>> GetInvestmentsTransactionsStats(int userId, int assetId, int referenceAssetId)
        {
            var splits = await _context.AssetSplitEvents
                .Where(s => s.AssetId == assetId)
                .Select(s => new { s.Date, s.SplitRatio })
                .ToListAsync();

            var refQuotes = await _context.AssetQuotes
                .Where(aq => aq.Asset.Id == referenceAssetId)
                .Where(aq => aq.Type == "NA" || aq.Type == "BLUE")
                .OrderByDescending(aq => aq.Date)
                .Select(aq => new { aq.Date, aq.Value })
                .ToListAsync();

            var rawData = await _context.InvestmentTransactions
                .Include(it => it.IncomeTransaction!).ThenInclude(t => t!.Account)
                .Include(it => it.ExpenseTransaction)
                .Where(it => it.IncomeTransaction!.UserId == userId || it.ExpenseTransaction!.UserId == userId)
                .Where(it => it.IncomeTransaction!.AssetId == assetId || it.ExpenseTransaction!.AssetId == assetId)
                .ToListAsync();

            decimal GetRefQuoteAt(DateTime date) =>
                refQuotes.FirstOrDefault(q => q.Date <= date)?.Value ?? 0m;

            decimal GetSplitFactor(DateTime date) =>
                splits
                    .Where(s => s.Date > date)
                    .Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            return rawData
                .Select(it =>
                {
                    bool isIncome = it.IncomeTransaction!.AssetId == assetId;
                    var tx = isIncome ? it.IncomeTransaction! : it.ExpenseTransaction!;
                    var factor = GetSplitFactor(tx.Date);
                    var refQuote = GetRefQuoteAt(it.IncomeTransaction!.Date);

                    var adjustedQty = Math.Abs(tx.Amount) * factor;
                    // QuotePrice stored as 1/price; display price = (1/storedQP)/factor * refQuote
                    var displayPrice = 1m / tx.QuotePrice!.Value / factor * refQuote;

                    return new InvestmentTransactionsResult
                    {
                        Date = it.IncomeTransaction!.Date,
                        Account = it.IncomeTransaction!.Account.Name,
                        MovementType = isIncome ? "I" : "E",
                        CommerceType = it.CommerceType,
                        Quantity = adjustedQty,
                        QuotePrice = displayPrice,
                        Total = Math.Abs(tx.Amount) * (1m / tx.QuotePrice!.Value) * refQuote
                    };
                })
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public async Task<decimal> GetAverageBuyValue(int userId, int assetId, int referenceAssetId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.QuotePrice.HasValue)
                .ToListAsync(); // Traer los datos a memoria antes de hacer c?lculos

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
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.PortfolioId == portfolioId)
                .Select(t => new { t.Amount, t.Date })
                .ToListAsync();

            var splits = await _context.AssetSplitEvents
                .Where(s => s.AssetId == assetId)
                .Select(s => new { s.Date, s.SplitRatio })
                .ToListAsync();

            return transactions.Sum(t =>
            {
                var factor = splits
                    .Where(s => s.Date > t.Date)
                    .Aggregate(1m, (acc, s) => acc * s.SplitRatio);
                return t.Amount * factor;
            });
        }


        // get average buy value for the asset in the account and portfolio combination
        public async Task<decimal> GetAverageQuotePrice(int accountId, int assetId, int portfolioId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .Where(t => t.AssetId == assetId)
                .Where(t => t.PortfolioId == portfolioId)
                .Where(t => t.QuotePrice.HasValue)
                .Select(t => new { t.QuotePrice, t.Date })
                .ToListAsync();

            if (transactions.Count == 0) return 0;

            var splits = await _context.AssetSplitEvents
                .Where(s => s.AssetId == assetId)
                .Select(s => new { s.Date, s.SplitRatio })
                .ToListAsync();

            // QuotePrice is stored as inverse rate (1/price). After a split, the equivalent
            // stored rate scales up by the factor (e.g. 4:1 split: 1/200 → 1/50 = (1/200)×4).
            return transactions.Average(t =>
            {
                var factor = splits
                    .Where(s => s.Date > t.Date)
                    .Aggregate(1m, (acc, s) => acc * s.SplitRatio);
                return t.QuotePrice.Value * factor;
            });
        }
    }
}
