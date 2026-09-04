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

        // classId/tagId/from/to: drill-down desde los reportes de Ingresos y Egresos (Fase 13) — clic en
        // una categoría o etiqueta lleva acá filtrado. Todos nullable y sin filtrar por default, así que
        // no cambia el comportamiento de la pantalla general de movimientos.
        public async Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetPaginatedTransactions(int userId, int page, int pageSize,
            int? classId = null, int? tagId = null, DateTime? from = null, DateTime? to = null)
        {


            var totalCount = await _context.Transactions
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentTransactions.Any(im => im.IncomeTransactionId == m.Id || im.ExpenseTransactionId == m.Id))
                .Where(m => classId == null || m.TransactionClassId == classId)
                .Where(m => from == null || m.Date >= from)
                .Where(m => to == null || m.Date < to)
                .Where(m => tagId == null || _context.TransactionTags.Any(tt => tt.TransactionId == m.Id && tt.TagId == tagId))
                .CountAsync();

            var transactions = await _context.Transactions
                .Where(m => m.Account.UserId == userId)
                .Where(m => m.TransactionClassId != null)
                .Where(m => m.MovementType == "E" || m.MovementType == "I")
                .Where(m => !_context.InvestmentTransactions.Any(im => im.IncomeTransactionId == m.Id || im.ExpenseTransactionId == m.Id))
                .Where(m => classId == null || m.TransactionClassId == classId)
                .Where(m => from == null || m.Date >= from)
                .Where(m => to == null || m.Date < to)
                .Where(m => tagId == null || _context.TransactionTags.Any(tt => tt.TransactionId == m.Id && tt.TagId == tagId))
                .Include(m => m.Account)
                .Include(m => m.Asset)
                .Include(m => m.TransactionClass)
                .Include(m => m.Portfolio)
                .Include(m => m.Trip)
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
                .Include(m => m.Trip)
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

        // Reemplaza a las tres ramas casi idénticas (pesos / dólares / cualquier otro activo) que tenía este
        // método antes (ver docs/plans/activos/reemplazar-stored-procedures.md, Fase 5). No eran un stored
        // procedure sino SQL crudo embebido, con la misma lógica copy-pasteada 3 veces distinguiendo por
        // Asset.Name en vez de un @ReferenceAssetId genérico como sí usan las stats de inversión.
        //
        // Particularidades del cálculo original preservadas:
        // 1. El activo con Id=2 (Dólar Estadounidense, confirmado contra datos reales) está hardcodeado como
        //    pivote: cualquier transacción de ese activo se suma directo, sin buscar cotización.
        // 2. Mejora de seguridad sin cambio de comportamiento observable: la búsqueda de cotización se
        //    resuelve con FirstOrDefault en vez de una subquery escalar — si hubiera más de una fila
        //    coincidente, el SP original tiraría "Subquery returned more than 1 value" (error 500); acá
        //    simplemente toma la primera.
        //
        // Desviación deliberada respecto al SP original (no es paridad estricta, decidido explícitamente):
        // el SP exigía que la cotización usada, tanto para valorizar cada tenencia como para la conversión
        // final a la moneda pedida, cayera EXACTAMENTE en la fecha más reciente de toda la tabla AssetQuotes
        // (compartida por todos los activos) — no la última cotización propia de cada uno, a diferencia de
        // GetStockStats (Fase 1). Verificado contra la base real: esto dejaba en $0 varios Fondos Comunes de
        // Inversión con tenencia real (se cotizan con menor frecuencia que acciones/cripto, ej. mensual) que
        // no tenían cotización justo en esa fecha global. Se cambia a "la última cotización propia de cada
        // activo" (mismo criterio que la Fase 1) para que esas tenencias sí se cuenten.
        public async Task<TotalsBalanceResult> GetTotalsBalanceByUserAsync(int userId, Asset asset)
        {
            const int DollarPivotAssetId = 2; // hardcodeado también en el SP original

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new { t.AssetId, t.Amount, t.Date })
                .ToListAsync();

            if (transactions.Count == 0)
                return new TotalsBalanceResult { Asset = asset.Name, Symbol = asset.Symbol, Color = asset.Color, Balance = 0m };

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();

            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            // Última cotización propia de cada activo (Type distinto de TARJETA/BLUE) para expresar su
            // tenencia en dólares. Se guarda la LISTA de valores que comparten esa fecha (no se suman entre
            // sí): si hay más de un Type el mismo día (ej. Peso Argentino con NA y BOLSA — pasa en 836 fechas
            // distintas en datos reales), cada uno aporta su propio cociente por separado.
            var latestQuotesByAsset = (await _context.AssetQuotes
                    .Where(q => assetIds.Contains(q.AssetId))
                    .Where(q => q.Type != "TARJETA" && q.Type != "BLUE")
                    .Select(q => new { q.AssetId, q.Date, q.Value })
                    .ToListAsync())
                .GroupBy(q => q.AssetId)
                .ToDictionary(g => g.Key, g => g.Where(q => q.Date == g.Max(x => x.Date)).Select(q => q.Value).ToList());

            var rawTotalInDollars = transactions.Sum(t =>
            {
                var factor = GetSplitFactor(t.AssetId, t.Date);
                if (t.AssetId == DollarPivotAssetId)
                    return t.Amount * factor;

                if (!latestQuotesByAsset.TryGetValue(t.AssetId, out var quotesAtLatestDate) || quotesAtLatestDate.Count == 0)
                    return 0m;

                return quotesAtLatestDate.Sum(quote => quote == 0 ? 0m : (t.Amount * factor) / quote);
            });

            // El SP original redondea el subtotal en USD a 2 decimales antes de convertir a la moneda pedida.
            var totalInDollars = Math.Round(rawTotalInDollars, 2);

            decimal balance;
            if (asset.Name == "Dolar Estadounidense")
            {
                balance = totalInDollars;
            }
            else
            {
                // Última cotización propia (Peso Argentino: su propio Type='BOLSA' más reciente — confirmado
                // que ningún otro activo usa ese Type en datos reales; "otro activo": su propia última
                // cotización de cualquier Type, igual que el SP original).
                var rate = asset.Name == "Peso Argentino"
                    ? await _context.AssetQuotes
                        .Where(q => q.AssetId == asset.Id && q.Type == "BOLSA")
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Value)
                        .FirstOrDefaultAsync()
                    : await _context.AssetQuotes
                        .Where(q => q.AssetId == asset.Id)
                        .OrderByDescending(q => q.Date)
                        .Select(q => (decimal?)q.Value)
                        .FirstOrDefaultAsync();

                balance = rate.HasValue ? totalInDollars * rate.Value : 0m;
            }

            return new TotalsBalanceResult
            {
                Asset = asset.Name,
                Symbol = asset.Symbol,
                Color = asset.Color,
                Balance = balance
            };
        }

        // ── Patrimonio (Fase 10) ──────────────────────────────────────────────
        // No reutiliza ni modifica GetTotalsBalanceByUserAsync (T7) — son cálculos nuevos, paralelos.
        // Mismo pivote dólar (Id 2) y mismo criterio de "última cotización propia" que ese método,
        // para que el resto de las aperturas de Patrimonio sea consistente con el total de "hoy".

        private const int NetWorthDollarPivotAssetId = 2;

        // Historial de cotizaciones propias de un activo, más reciente primero — mismo criterio que
        // el bloque final de GetTotalsBalanceByUserAsync (Peso Argentino: su Type='BOLSA'; el resto,
        // cualquier Type), generalizado para poder resolver "a una fecha" y no solo "hoy".
        private async Task<List<(DateTime Date, decimal Value)>> GetOwnRateHistoryAsync(Asset asset)
        {
            if (asset.Name == "Dolar Estadounidense") return new List<(DateTime, decimal)>();

            var query = asset.Name == "Peso Argentino"
                ? _context.AssetQuotes.Where(q => q.AssetId == asset.Id && q.Type == "BOLSA")
                : _context.AssetQuotes.Where(q => q.AssetId == asset.Id);

            return (await query.OrderByDescending(q => q.Date).Select(q => new { q.Date, q.Value }).ToListAsync())
                .Select(q => (q.Date, q.Value))
                .ToList();
        }

        private static (decimal? Rate, DateTime? Date) GetRateOnOrBefore(Asset asset, List<(DateTime Date, decimal Value)> history, DateTime date)
        {
            if (asset.Name == "Dolar Estadounidense") return (1m, null);
            foreach (var q in history)
                if (q.Date <= date) return (q.Value, q.Date);
            return (null, null);
        }

        public async Task<(decimal Rate, DateTime? QuoteDate)> GetReferenceAssetRateAsync(Asset asset)
        {
            var history = await GetOwnRateHistoryAsync(asset);
            var (rate, date) = GetRateOnOrBefore(asset, history, DateTime.Today);
            return (rate ?? 0m, date);
        }

        public async Task<IEnumerable<StaleAssetResult>> GetStaleAssetsAsync(int userId, int staleDaysThreshold)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.AssetId != NetWorthDollarPivotAssetId)
                .Select(t => new { t.AssetId, AssetName = t.Asset.Name, t.Amount, t.Date })
                .ToListAsync();

            if (transactions.Count == 0) return Enumerable.Empty<StaleAssetResult>();

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();
            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();
            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            // Solo interesa lo que hoy sigue en cartera — un activo ya vendido del todo no debería
            // avisar por una cotización vieja que ya no valúa nada.
            var byAsset = transactions.GroupBy(t => t.AssetId).ToDictionary(g => g.Key, g => new { g.First().AssetName, Rows = g.ToList() });
            var heldAssetIds = byAsset
                .Where(kv => kv.Value.Rows.Sum(t => t.Amount * GetSplitFactor(kv.Key, t.Date)) != 0)
                .Select(kv => kv.Key)
                .ToList();

            if (heldAssetIds.Count == 0) return Enumerable.Empty<StaleAssetResult>();

            var latestQuoteByAsset = (await _context.AssetQuotes
                    .Where(q => heldAssetIds.Contains(q.AssetId))
                    .Where(q => q.Type != "TARJETA" && q.Type != "BLUE")
                    .Select(q => new { q.AssetId, q.Date })
                    .ToListAsync())
                .GroupBy(q => q.AssetId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.Date));

            var cutoff = DateTime.Today.AddDays(-staleDaysThreshold);

            return heldAssetIds
                .Where(id => latestQuoteByAsset.TryGetValue(id, out var date) && date < cutoff)
                .Select(id => new StaleAssetResult { AssetName = byAsset[id].AssetName, QuoteDate = latestQuoteByAsset[id] })
                .OrderBy(r => r.QuoteDate)
                .ToList();
        }

        private async Task<Dictionary<int, List<(DateTime Date, decimal Value)>>> GetQuotesByAssetAsync(IEnumerable<int> assetIds)
        {
            return (await _context.AssetQuotes
                    .Where(q => assetIds.Contains(q.AssetId))
                    .Where(q => q.Type != "TARJETA" && q.Type != "BLUE")
                    .Select(q => new { q.AssetId, q.Date, q.Value })
                    .ToListAsync())
                .GroupBy(q => q.AssetId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(q => q.Date).Select(q => (q.Date, q.Value)).ToList());
        }

        private static List<decimal> GetAssetQuotesOnOrBefore(Dictionary<int, List<(DateTime Date, decimal Value)>> quotesByAsset, int assetId, DateTime date)
        {
            if (!quotesByAsset.TryGetValue(assetId, out var quotes)) return new List<decimal>();
            var match = quotes.FirstOrDefault(q => q.Date <= date);
            return match == default ? new List<decimal>() : quotes.Where(q => q.Date == match.Date).Select(q => q.Value).ToList();
        }

        private static decimal ToUsd(int assetId, decimal nativeAmount, Dictionary<int, List<(DateTime Date, decimal Value)>> quotesByAsset, DateTime date)
        {
            if (nativeAmount == 0) return 0m;
            if (assetId == NetWorthDollarPivotAssetId) return nativeAmount;
            var quotes = GetAssetQuotesOnOrBefore(quotesByAsset, assetId, date);
            if (quotes.Count == 0) return 0m;
            return quotes.Where(q => q != 0).Sum(q => nativeAmount / q);
        }

        // Mismo criterio de stablecoin que el resto de los reportes de cripto (GetInvestmentValueContributionsAsync).
        private static readonly string[] StableCryptoSymbols = { "DAI", "USDT", "USDC" };

        private static string ClassifyNetWorthBucket(string assetTypeName, string environment, string assetSymbol)
        {
            if (environment == "FIAT") return "Accounts";
            if (environment == "CRYPTO") return StableCryptoSymbols.Contains(assetSymbol) ? "CryptoStable" : "CryptoVolatile";
            if (assetTypeName == "Bono" || assetTypeName == "Obligacion Negociable") return "Bonds"; // renta fija
            return "Stocks"; // renta variable: Accion Argentina, CEDEAR, FCI, Accion USA
        }

        // Devuelve, de más viejo a más nuevo, los cortes de fecha de los últimos `months` meses: fin de
        // cada mes salvo el último punto, que es hoy (mes en curso, todavía no cerrado).
        private static List<(DateTime Cutoff, DateTime MonthLabel)> GetMonthlyCutoffs(int months)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var cutoffs = new List<(DateTime, DateTime)>();
            for (int i = months - 1; i >= 0; i--)
            {
                if (i == 0) { cutoffs.Add((today, currentMonthStart)); continue; }
                var monthStart = currentMonthStart.AddMonths(-i);
                cutoffs.Add((monthStart.AddMonths(1).AddDays(-1), monthStart));
            }
            return cutoffs;
        }

        public async Task<IEnumerable<NetWorthMonthlyPointResult>> GetNetWorthMonthlySeriesAsync(int userId, Asset referenceAsset, int months)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new { t.AssetId, t.Amount, t.Date, AssetTypeName = t.Asset.AssetType.Name, Environment = t.Asset.AssetType.Environment, AssetSymbol = t.Asset.Symbol })
                .ToListAsync();

            if (transactions.Count == 0) return Enumerable.Empty<NetWorthMonthlyPointResult>();

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();
            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();
            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            var byAsset = transactions.GroupBy(t => t.AssetId).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());
            var bucketByAsset = transactions.GroupBy(t => t.AssetId)
                .ToDictionary(g => g.Key, g => ClassifyNetWorthBucket(g.First().AssetTypeName, g.First().Environment, g.First().AssetSymbol));

            var quotesByAsset = await GetQuotesByAssetAsync(assetIds);
            var referenceHistory = await GetOwnRateHistoryAsync(referenceAsset);

            var points = new List<NetWorthMonthlyPointResult>();
            foreach (var (cutoff, monthLabel) in GetMonthlyCutoffs(months))
            {
                var (refRate, _) = GetRateOnOrBefore(referenceAsset, referenceHistory, cutoff);
                decimal accounts = 0, stocks = 0, cryptoStable = 0, cryptoVolatile = 0, bonds = 0;

                foreach (var assetId in assetIds)
                {
                    var nativeAmount = byAsset[assetId].Where(t => t.Date <= cutoff).Sum(t => t.Amount * GetSplitFactor(assetId, t.Date));
                    if (nativeAmount == 0) continue;

                    var usd = ToUsd(assetId, nativeAmount, quotesByAsset, cutoff);
                    var inReference = refRate.HasValue ? usd * refRate.Value : 0m;

                    switch (bucketByAsset[assetId])
                    {
                        case "Accounts": accounts += inReference; break;
                        case "CryptoStable": cryptoStable += inReference; break;
                        case "CryptoVolatile": cryptoVolatile += inReference; break;
                        case "Bonds": bonds += inReference; break;
                        default: stocks += inReference; break;
                    }
                }

                points.Add(new NetWorthMonthlyPointResult
                {
                    Month = monthLabel,
                    Accounts = Math.Round(accounts, 2),
                    Stocks = Math.Round(stocks, 2),
                    CryptoStable = Math.Round(cryptoStable, 2),
                    CryptoVolatile = Math.Round(cryptoVolatile, 2),
                    Bonds = Math.Round(bonds, 2)
                });
            }

            return points;
        }

        public async Task<IEnumerable<AccountBalanceResult>> GetAccountBalancesAsync(int userId, Asset referenceAsset, int evolutionMonths)
        {
            var rows = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new { t.AccountId, AccountName = t.Account.Name, t.AssetId, AssetName = t.Asset.Name, AssetSymbol = t.Asset.Symbol, AssetTypeName = t.Asset.AssetType.Name, t.Amount, t.Date })
                .ToListAsync();

            if (rows.Count == 0) return Enumerable.Empty<AccountBalanceResult>();

            var assetIds = rows.Select(r => r.AssetId).Distinct().ToList();
            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();
            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            var quotesByAsset = await GetQuotesByAssetAsync(assetIds);
            var referenceHistory = await GetOwnRateHistoryAsync(referenceAsset);

            decimal ToReference(int assetId, decimal nativeAmount, DateTime asOf)
            {
                var usd = ToUsd(assetId, nativeAmount, quotesByAsset, asOf);
                var (rate, _) = GetRateOnOrBefore(referenceAsset, referenceHistory, asOf);
                return rate.HasValue ? usd * rate.Value : 0m;
            }

            var byAccountAsset = rows.GroupBy(r => (r.AccountId, r.AssetId))
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());

            var today = DateTime.Today;
            var result = rows
                .GroupBy(r => new { r.AccountId, r.AccountName })
                .Select(accGroup =>
                {
                    var holdings = accGroup
                        .Select(x => x.AssetId).Distinct()
                        .Select(assetId =>
                        {
                            var info = accGroup.First(x => x.AssetId == assetId);
                            var native = byAccountAsset[(accGroup.Key.AccountId, assetId)]
                                .Where(x => x.Date <= today)
                                .Sum(x => x.Amount * GetSplitFactor(assetId, x.Date));
                            return new AccountHoldingResult
                            {
                                AssetId = assetId,
                                AssetName = info.AssetName,
                                AssetSymbol = info.AssetSymbol,
                                AssetTypeName = info.AssetTypeName,
                                // Sin redondear a 2 decimales: una tenencia cripto chica (ej. 0,0022709 BTC en
                                // BuenBit) redondea a 0,00 y el filtro de abajo la descarta silenciosamente —
                                // confirmado real en producción. GetBalanceByAssetAndUserAsync (viejo "Saldos")
                                // nunca redondeaba este número por la misma razón.
                                NativeBalance = native,
                                BalanceInReferenceAsset = Math.Round(ToReference(assetId, native, today), 2)
                            };
                        })
                        .Where(h => h.NativeBalance != 0)
                        .OrderByDescending(h => h.BalanceInReferenceAsset)
                        .ToList();

                    return new AccountBalanceResult
                    {
                        AccountId = accGroup.Key.AccountId,
                        AccountName = accGroup.Key.AccountName,
                        Balance = holdings.Sum(h => h.BalanceInReferenceAsset),
                        Holdings = holdings
                    };
                })
                .Where(a => a.Holdings.Count > 0)
                .ToList();

            foreach (var account in result)
            {
                var accountAssetIds = account.Holdings.Select(h => h.AssetId).ToList();
                foreach (var (cutoff, monthLabel) in GetMonthlyCutoffs(evolutionMonths))
                {
                    var total = accountAssetIds.Sum(assetId =>
                    {
                        var native = byAccountAsset[(account.AccountId, assetId)]
                            .Where(x => x.Date <= cutoff)
                            .Sum(x => x.Amount * GetSplitFactor(assetId, x.Date));
                        return ToReference(assetId, native, cutoff);
                    });
                    account.Evolution.Add(new MonthlyBalanceResult { Month = monthLabel, Balance = Math.Round(total, 2) });
                }
            }

            return result.OrderByDescending(a => a.Balance).ToList();
        }

        // ── Ingresos y Egresos (Fase 12) ─────────────────────────────────────────────────────────
        // Mismas guardas T1/T2 que GetIncExpStatsAsync (TransactionClassId != null, CountsAsIncomeExpense,
        // sin excluir CardTransactionId — el gasto es la cuota, cuando se paga). La conversión de moneda
        // es la misma lógica de GetIncExpStatsAsync, extraída acá para no repetirla cinco veces.

        private const string MOV_INCOME = "I";
        private const string MOV_EXPENSE = "E";

        private class IncExpRawRow
        {
            public string MovementType { get; set; } = "";
            public string ClassName { get; set; } = "";
            public int AssetId { get; set; }
            public decimal Amount { get; set; }
            public decimal? QuotePrice { get; set; }
            public DateTime Date { get; set; }
        }

        // Devuelve un conversor (assetId origen, monto, cotización origen, fecha) -> monto en `targetAsset`,
        // con la misma política que GetIncExpStatsAsync: si el destino es ARS se usa la serie BLUE, si no,
        // la serie propia del destino; última cotización <= fecha, sin dividir por cero.
        private async Task<Func<int, decimal, decimal?, DateTime, decimal>> BuildCurrencyConverterAsync(Asset targetAsset)
        {
            const string ARS_NAME = "Peso Argentino";
            const string BLUE = "BLUE";
            var isTargetARS = string.Equals(targetAsset.Name, ARS_NAME, StringComparison.OrdinalIgnoreCase);

            var quotesQuery = isTargetARS
                ? _context.AssetQuotes.AsNoTracking().Where(aq => aq.Asset.Name == ARS_NAME && aq.Type == BLUE)
                : _context.AssetQuotes.AsNoTracking().Where(aq => aq.Asset.Name == targetAsset.Name);

            var quotes = (await quotesQuery
                .OrderBy(aq => aq.Date)
                .Select(aq => new { aq.Date, aq.Value })
                .ToListAsync())
                .Select(x => (x.Date, x.Value))
                .ToList();

            decimal GetQuoteAt(DateTime date)
            {
                if (quotes.Count == 0) return 1m;
                int lo = 0, hi = quotes.Count - 1, best = -1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    if (quotes[mid].Date <= date) { best = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                return best >= 0 ? quotes[best].Value : quotes[0].Value;
            }

            return (sourceAssetId, amount, sourceQuotePrice, date) =>
            {
                if (sourceAssetId == targetAsset.Id) return amount;
                var srcQuote = sourceQuotePrice ?? 0m;
                if (srcQuote <= 0m) return 0m;
                return amount / srcQuote * GetQuoteAt(date);
            };
        }

        // Serie de cotizaciones de un tipo dado, por asset de origen — para convertir montos que no
        // traen su propia cotización guardada (CardTransaction no tiene QuotePrice, a diferencia de
        // Transaction). Misma búsqueda binaria "última <= fecha" que BuildCurrencyConverterAsync.
        private async Task<Dictionary<int, List<(DateTime Date, decimal Value)>>> LoadQuoteSeriesByAssetAsync(IEnumerable<int> assetIds, string type)
        {
            var ids = assetIds.Distinct().ToList();
            if (ids.Count == 0) return new();

            var rows = await _context.AssetQuotes.AsNoTracking()
                .Where(aq => ids.Contains(aq.AssetId) && aq.Type == type)
                .OrderBy(aq => aq.Date)
                .Select(aq => new { aq.AssetId, aq.Date, aq.Value })
                .ToListAsync();

            return rows.GroupBy(r => r.AssetId)
                .ToDictionary(g => g.Key, g => g.Select(x => (x.Date, x.Value)).ToList());
        }

        private static decimal LookupQuote(List<(DateTime Date, decimal Value)> series, DateTime date)
        {
            if (series.Count == 0) return 0m;
            int lo = 0, hi = series.Count - 1, best = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (series[mid].Date <= date) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return best >= 0 ? series[best].Value : series[0].Value;
        }

        // Resumen del mes en cascada: ingresos del mes, un escalón por categoría de egreso (mayor a
        // menor) y el resultado del mes anterior, para la comparación siempre presente (sección 7).
        public async Task<IncExpWaterfallResult> GetIncExpWaterfallAsync(int userId, DateTime month, Asset asset)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var prevMonthStart = monthStart.AddMonths(-1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            (t.MovementType == MOV_INCOME || t.MovementType == MOV_EXPENSE) &&
                            t.Date >= prevMonthStart && t.Date < monthEnd &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new IncExpRawRow { MovementType = t.MovementType, ClassName = t.TransactionClass.Description, AssetId = t.AssetId, Amount = t.Amount, QuotePrice = t.QuotePrice, Date = t.Date })
                .ToListAsync();

            decimal SumConverted(IEnumerable<IncExpRawRow> rows) => rows.Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date));

            var totalIncome = Math.Round(SumConverted(raw.Where(x => x.MovementType == MOV_INCOME && x.Date >= monthStart && x.Date < monthEnd)), 2);

            var steps = raw
                .Where(x => x.MovementType == MOV_EXPENSE && x.Date >= monthStart && x.Date < monthEnd)
                .GroupBy(x => x.ClassName)
                .Select(g => new WaterfallStepResult
                {
                    CategoryName = g.Key,
                    Amount = Math.Round(Math.Abs(g.Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date))), 2)
                })
                .OrderByDescending(s => s.Amount)
                .ToList();

            var totalExpense = steps.Sum(s => s.Amount);

            var prevIncome = Math.Round(SumConverted(raw.Where(x => x.MovementType == MOV_INCOME && x.Date >= prevMonthStart && x.Date < monthStart)), 2);
            var prevExpense = Math.Round(Math.Abs(SumConverted(raw.Where(x => x.MovementType == MOV_EXPENSE && x.Date >= prevMonthStart && x.Date < monthStart))), 2);

            return new IncExpWaterfallResult
            {
                Month = monthStart,
                TotalIncome = totalIncome,
                ExpenseSteps = steps,
                TotalExpense = totalExpense,
                Result = Math.Round(totalIncome - totalExpense, 2),
                PreviousMonthResult = Math.Round(prevIncome - prevExpense, 2)
            };
        }

        // Serie mensual de ingreso/egreso — evolución + tendencia (D-A: el promedio móvil se arma en
        // el service sobre esta serie, es aritmética pura y no necesita otra consulta).
        public async Task<IEnumerable<IncExpEvolutionPointResult>> GetIncExpEvolutionAsync(int userId, Asset asset, int months)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var cutoff = currentMonthStart.AddMonths(-(months - 1));
            var rangeEnd = currentMonthStart.AddMonths(1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            (t.MovementType == MOV_INCOME || t.MovementType == MOV_EXPENSE) &&
                            t.Date >= cutoff && t.Date < rangeEnd &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new { t.MovementType, t.AssetId, t.Amount, t.QuotePrice, t.Date })
                .ToListAsync();

            var points = new List<IncExpEvolutionPointResult>();
            for (int i = months - 1; i >= 0; i--)
            {
                var bucketStart = currentMonthStart.AddMonths(-i);
                var bucketEnd = bucketStart.AddMonths(1);

                var income = Math.Round(raw.Where(x => x.MovementType == MOV_INCOME && x.Date >= bucketStart && x.Date < bucketEnd)
                    .Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date)), 2);
                var expense = Math.Round(Math.Abs(raw.Where(x => x.MovementType == MOV_EXPENSE && x.Date >= bucketStart && x.Date < bucketEnd)
                    .Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date))), 2);

                points.Add(new IncExpEvolutionPointResult { Month = bucketStart, Income = income, Expense = expense, Result = Math.Round(income - expense, 2) });
            }

            return points;
        }

        // Gasto por categoría con su mini-serie mensual (D-1/D-2: el agrupado por rubro sale del
        // ParentId en el service — acá alcanza con traer el padre de cada categoría, un solo hop por T4).
        public async Task<IEnumerable<CategorySpendingResult>> GetSpendingByCategoryMonthlySeriesAsync(int userId, Asset asset, DateTime month, int months)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var cutoff = monthStart.AddMonths(-(months - 1));

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= cutoff && t.Date < monthEnd &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new
                {
                    ClassId = t.TransactionClassId!.Value,
                    ClassName = t.TransactionClass.Description,
                    ParentId = t.TransactionClass.ParentId,
                    ParentName = t.TransactionClass.Parent != null ? t.TransactionClass.Parent.Description : null,
                    t.AssetId,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            var result = new List<CategorySpendingResult>();
            foreach (var g in raw.GroupBy(x => new { x.ClassId, x.ClassName, x.ParentId, x.ParentName }))
            {
                var trend = new List<decimal>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var bucketStart = monthStart.AddMonths(-i);
                    var bucketEnd = bucketStart.AddMonths(1);
                    var sum = g.Where(x => x.Date >= bucketStart && x.Date < bucketEnd)
                        .Sum(x => Math.Abs(convert(x.AssetId, x.Amount, x.QuotePrice, x.Date)));
                    trend.Add(Math.Round(sum, 2));
                }

                result.Add(new CategorySpendingResult
                {
                    CategoryId = g.Key.ClassId,
                    CategoryName = g.Key.ClassName,
                    ParentId = g.Key.ParentId,
                    ParentName = g.Key.ParentName,
                    MonthlyTrend = trend
                });
            }

            return result;
        }

        // Por etiqueta (D-4): combina movimientos de cuenta etiquetados (Transaction.QuotePrice propio)
        // con consumos de tarjeta etiquetados (CardTransaction no tiene QuotePrice — se resuelve con la
        // cotización "TARJETA" del asset de origen, mismo criterio que GetLiveCardDebtInDollarsAsync).
        // Un gasto de tarjeta se etiqueta siempre en el CardTransaction, nunca en una cuota (CLAUDE.md).
        public async Task<IEnumerable<TagSpendingResult>> GetSpendingByTagAsync(int userId, Asset asset, int months)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var cutoff = currentMonthStart.AddMonths(-(months - 1));
            var rangeEnd = currentMonthStart.AddMonths(1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var transactionRows = await _context.TransactionTags
                .AsNoTracking()
                .Where(tt => tt.Transaction.UserId == userId &&
                             tt.Transaction.MovementType == MOV_EXPENSE &&
                             tt.Transaction.TransactionClassId != null &&
                             tt.Transaction.Date >= cutoff && tt.Transaction.Date < rangeEnd &&
                             tt.Transaction.TransactionClass!.CountsAsIncomeExpense)
                .Select(tt => new
                {
                    tt.TagId,
                    TagName = tt.Tag.Name,
                    tt.Tag.Color,
                    ClassName = tt.Transaction.TransactionClass!.Description,
                    tt.Transaction.AssetId,
                    tt.Transaction.Amount,
                    tt.Transaction.QuotePrice,
                    tt.Transaction.Date
                })
                .ToListAsync();

            var cardRowsRaw = await _context.CardTransactionTags
                .AsNoTracking()
                .Where(ct => ct.CardTransaction.UserId == userId &&
                             ct.CardTransaction.Date >= cutoff && ct.CardTransaction.Date < rangeEnd)
                .Select(ct => new
                {
                    ct.TagId,
                    TagName = ct.Tag.Name,
                    ct.Tag.Color,
                    ClassName = ct.CardTransaction.TransactionClass.Description,
                    ct.CardTransaction.AssetId,
                    Amount = ct.CardTransaction.TotalAmount,
                    ct.CardTransaction.Date
                })
                .ToListAsync();

            var cardQuoteSeries = await LoadQuoteSeriesByAssetAsync(cardRowsRaw.Select(x => x.AssetId), "TARJETA");

            var combined = transactionRows
                .Select(x => (x.TagId, x.TagName, x.Color, x.ClassName, x.Date,
                    Amount: Math.Abs(convert(x.AssetId, x.Amount, x.QuotePrice, x.Date))))
                .Concat(cardRowsRaw.Select(x => (x.TagId, x.TagName, x.Color, x.ClassName, x.Date,
                    Amount: Math.Abs(convert(x.AssetId, x.Amount, LookupQuote(cardQuoteSeries.GetValueOrDefault(x.AssetId, new()), x.Date), x.Date)))))
                .ToList();

            var result = new List<TagSpendingResult>();
            foreach (var g in combined.GroupBy(x => new { x.TagId, x.TagName, x.Color }))
            {
                var evolution = new List<MonthlyAmountResult>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var bucketStart = currentMonthStart.AddMonths(-i);
                    var bucketEnd = bucketStart.AddMonths(1);
                    var sum = g.Where(x => x.Date >= bucketStart && x.Date < bucketEnd).Sum(x => x.Amount);
                    evolution.Add(new MonthlyAmountResult { Month = bucketStart, Amount = Math.Round(sum, 2) });
                }

                var byCategory = g.GroupBy(x => x.ClassName)
                    .Select(cg => new CategoryAmountResult { CategoryName = cg.Key, Amount = Math.Round(cg.Sum(x => x.Amount), 2) })
                    .OrderByDescending(c => c.Amount)
                    .ToList();

                result.Add(new TagSpendingResult
                {
                    TagId = g.Key.TagId,
                    TagName = g.Key.TagName,
                    Color = g.Key.Color,
                    TotalAmount = Math.Round(g.Sum(x => x.Amount), 2),
                    MonthlyEvolution = evolution,
                    ByCategory = byCategory
                });
            }

            return result.OrderByDescending(r => r.TotalAmount).ToList();
        }

        // Calendario de gastos: un monto por día del año pedido (T1/T2), para el mapa de calor y el
        // promedio por día de semana, que se calcula en el service (aritmética pura sobre esto).
        public async Task<IEnumerable<DailySpendingResult>> GetDailySpendingAsync(int userId, Asset asset, int year)
        {
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= yearStart && t.Date < yearEnd &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new { t.AssetId, t.Amount, t.QuotePrice, t.Date })
                .ToListAsync();

            return raw
                .GroupBy(x => x.Date.Date)
                .Select(g => new DailySpendingResult
                {
                    Date = g.Key,
                    Amount = Math.Round(Math.Abs(g.Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date))), 2)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        // Ingresos (corrección 2026-09-04 sobre la Fase 13): evolución por categoría en el tiempo,
        // en vez de la composición de un mes — misma guarda T1/T2 que el resto, pero MOV_INCOME.
        public async Task<IEnumerable<IncomeCategorySeriesResult>> GetIncomeByCategoryMonthlySeriesAsync(int userId, Asset asset, int months)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var cutoff = currentMonthStart.AddMonths(-(months - 1));
            var rangeEnd = currentMonthStart.AddMonths(1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= cutoff && t.Date < rangeEnd &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new { ClassId = t.TransactionClassId!.Value, ClassName = t.TransactionClass.Description, t.AssetId, t.Amount, t.QuotePrice, t.Date })
                .ToListAsync();

            var result = new List<IncomeCategorySeriesResult>();
            foreach (var g in raw.GroupBy(x => new { x.ClassId, x.ClassName }))
            {
                var trend = new List<decimal>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var bucketStart = currentMonthStart.AddMonths(-i);
                    var bucketEnd = bucketStart.AddMonths(1);
                    var sum = g.Where(x => x.Date >= bucketStart && x.Date < bucketEnd)
                        .Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date));
                    trend.Add(Math.Round(sum, 2));
                }

                result.Add(new IncomeCategorySeriesResult { CategoryId = g.Key.ClassId, CategoryName = g.Key.ClassName, MonthlyTrend = trend });
            }

            return result;
        }

        // Días de cobro: monto de ingreso por día calendario, en la ventana de meses pedida — el
        // agrupado por día-del-mes (con su frecuencia) se arma en el service, es aritmética pura.
        public async Task<IEnumerable<DailySpendingResult>> GetDailyIncomeAsync(int userId, Asset asset, int months)
        {
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var start = currentMonthStart.AddMonths(-(months - 1));
            var end = currentMonthStart.AddMonths(1);

            var convert = await BuildCurrencyConverterAsync(asset);

            var raw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_INCOME &&
                            t.Date >= start && t.Date < end &&
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new { t.AssetId, t.Amount, t.QuotePrice, t.Date })
                .ToListAsync();

            return raw
                .GroupBy(x => x.Date.Date)
                .Select(g => new DailySpendingResult
                {
                    Date = g.Key,
                    Amount = Math.Round(g.Sum(x => convert(x.AssetId, x.Amount, x.QuotePrice, x.Date)), 2)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        public async Task<IncExpResult> GetDollarIncExpStatsAsync(int userId, DateTime month)
        {
            var dollarClassIncomeStats = await _context.Transactions
                .Include(t => t.TransactionClass)
                .Where(t => t.TransactionClassId != null)
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                .Where(t => t.TransactionClass.CountsAsIncomeExpense)
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
                            t.TransactionClass.CountsAsIncomeExpense)
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
                            t.TransactionClass.CountsAsIncomeExpense)
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
                            t.TransactionClass.CountsAsIncomeExpense)
                .Select(t => new { t.Date, t.AssetId, t.Amount, t.QuotePrice })
                .ToListAsync();

            var expensesSeriesRaw = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId &&
                            t.TransactionClassId != null &&
                            t.MovementType == MOV_EXPENSE &&
                            t.Date >= cutoff &&
                            t.TransactionClass.CountsAsIncomeExpense)
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
            public int PortfolioId { get; set; }
            public int AssetId { get; set; }
            public string AssetName { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string AssetTypeName { get; set; } = "";
            public int AccountId { get; set; }
            public string AccountName { get; set; } = "";
            public decimal QuantityContribution { get; set; }
            public decimal OriginalValueContribution { get; set; }
            public decimal ActualValueContribution { get; set; }
        }

        // environment == null: sin filtrar por ambiente (usado por las stats de cartera, que combinan
        // efectivo e inversión de cualquier tipo — ver docs/plans/activos/portfolios-estadisticas.md).
        // portfolioId == null: sin filtrar por cartera (usado por el resumen de todas las carteras, Fase 1);
        // con valor, acota la consulta a una sola cartera (Fase 2, detalle/holdings).
        private async Task<List<InvestmentValueContribution>> GetInvestmentValueContributionsAsync(
            int userId, string? environment, int referenceAssetId, int assetTypeId, bool considerStable, int? portfolioId = null)
        {
            var stableSymbols = new[] { "DAI", "USDT", "USDC" };

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Where(t => portfolioId == null || t.PortfolioId == portfolioId)
                .Where(t => environment == null || t.Asset.AssetType.Environment == environment)
                .Where(t => assetTypeId == 0 || t.Asset.AssetTypeId == assetTypeId)
                .Where(t => considerStable || !stableSymbols.Contains(t.Asset.Symbol))
                .Select(t => new
                {
                    t.PortfolioId,
                    t.AssetId,
                    AssetName = t.Asset.Name,
                    t.Asset.Symbol,
                    AssetTypeName = t.Asset.AssetType.Name,
                    t.AccountId,
                    AccountName = t.Account.Name,
                    t.Amount,
                    t.QuotePrice,
                    t.Date
                })
                .ToListAsync();

            if (transactions.Count == 0)
                return new List<InvestmentValueContribution>();

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();

            // Última cotización de cada activo, sin filtrar por Type (igual que el LEFT JOIN del SP
            // original, que resuelve por MAX(Date) sin distinguir Type). Se guarda la LISTA de valores
            // que comparten esa fecha (no se suman entre sí): con environment == null (stats de cartera)
            // un activo como Peso Argentino puede tener más de un Type el mismo día (ej. NA y BOLSA — 836
            // fechas distintas en datos reales, ver lección de fan-out en el plan), y cada uno debe aportar
            // su propio cociente por separado — sumar primero los valores y dividir una sola vez da un
            // resultado equivocado. Para Bolsa/Cripto (un solo Type por día en la práctica) esto no cambia
            // el resultado respecto al comportamiento anterior.
            // El MAX(Date) se resuelve con una subquery correlacionada en el propio SQL Server (igual
            // que el SP original) en vez de traer todo el historial de cotizaciones a memoria.
            var latestQuotesByAsset = (await _context.AssetQuotes
                    .Where(q => assetIds.Contains(q.AssetId))
                    .Where(q => q.Date == _context.AssetQuotes
                        .Where(q2 => q2.AssetId == q.AssetId)
                        .Max(q2 => q2.Date))
                    .Select(q => new { q.AssetId, q.Value })
                    .ToListAsync())
                .GroupBy(q => q.AssetId)
                .ToDictionary(g => g.Key, g => g.Select(q => q.Value).ToList());

            // Sin acotar por fecha mínima (bug real encontrado y corregido: acotar por
            // "Date >= earliestTransactionDate" asumía que nunca hace falta una cotización anterior a la
            // transacción más antigua, pero eso solo vale si la cotización de referencia no tiene huecos.
            // El Dólar, en datos reales, tiene una única cotización en el año 2000 y después un salto
            // directo a abril de 2024 — cualquier transacción fechada en ese hueco (2020-2023) quedaba sin
            // cotización de referencia válida porque la única candidata anterior (año 2000) se excluía por
            // este límite, dando OriginalValue = 0 para esas transacciones y, al mezclarse con otras del
            // mismo activo que sí resuelven bien, un total sin sentido — se detectó con una cartera cuyo
            // "Valor Original" de SPY daba negativo). El activo de referencia tiene relativamente pocas
            // cotizaciones (cientos, no miles), así que traer todo el historial es barato.
            var referenceQuotes = await _context.AssetQuotes
                .Where(q => q.AssetId == referenceAssetId && (q.Type == "BLUE" || q.Type == "NA"))
                .OrderByDescending(q => q.Date)
                .Select(q => new { q.Date, q.Value })
                .ToListAsync();

            // Mismo criterio de fan-out que latestQuotesByAsset, aplicado a la cotización de referencia:
            // todas las que comparten la fecha resuelta aportan su propio valor por separado.
            List<decimal> GetReferenceQuotesOnOrBefore(DateTime date)
            {
                var mostRecentMatch = referenceQuotes.FirstOrDefault(q => q.Date <= date);
                if (mostRecentMatch == null) return new List<decimal>();
                return referenceQuotes.Where(q => q.Date == mostRecentMatch.Date).Select(q => q.Value).ToList();
            }

            var latestReferenceDate = referenceQuotes.FirstOrDefault()?.Date;
            var latestReferenceQuotes = latestReferenceDate.HasValue
                ? referenceQuotes.Where(q => q.Date == latestReferenceDate.Value).Select(q => q.Value).ToList()
                : new List<decimal> { 1m };

            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            return transactions
                .Select(t =>
                {
                    var cumulativeFactor = GetSplitFactor(t.AssetId, t.Date);
                    var quantity = t.Amount * cumulativeFactor;

                    var referenceQuotesOnDate = GetReferenceQuotesOnOrBefore(t.Date);
                    var originalValue = t.QuotePrice.HasValue && t.QuotePrice.Value > 0 && referenceQuotesOnDate.Count > 0
                        ? referenceQuotesOnDate.Sum(refQuote => (t.Amount / t.QuotePrice.Value) * refQuote)
                        : 0m;

                    var latestAssetQuotes = latestQuotesByAsset.TryGetValue(t.AssetId, out var quotes) ? quotes : new List<decimal>();
                    var actualValue = latestAssetQuotes
                        .Where(assetQuote => assetQuote > 0)
                        .SelectMany(assetQuote => latestReferenceQuotes.Select(refQuote => (quantity / assetQuote) * refQuote))
                        .Sum();

                    return new InvestmentValueContribution
                    {
                        PortfolioId = t.PortfolioId,
                        AssetId = t.AssetId,
                        AssetName = t.AssetName,
                        Symbol = t.Symbol,
                        AssetTypeName = t.AssetTypeName,
                        AccountId = t.AccountId,
                        AccountName = t.AccountName,
                        QuantityContribution = quantity,
                        OriginalValueContribution = originalValue,
                        ActualValueContribution = actualValue
                    };
                })
                .ToList();
        }

        // Resumen de valor por cartera (ver docs/plans/activos/portfolios-estadisticas.md, Fase 1).
        // A diferencia de GetStockStatsAsync/GetStocksGralStatsAsync, combina todo tipo de activo (efectivo
        // e inversión, sin filtro de Environment ni AssetTypeId) porque una cartera es transversal a ambos.
        // Las stablecoins siempre se cuentan (ConsiderStable fijo en true, sin toggle expuesto — Decisión 5
        // del plan). Devuelve una fila por cada cartera del usuario, incluyendo las que no tienen ninguna
        // transacción (valor $0, no se excluyen).
        public async Task<IEnumerable<PortfolioStatsResult>> GetPortfolioStatsAsync(int userId, int referenceAssetId)
        {
            var portfolios = await _context.Portfolios
                .Where(p => p.UserId == userId)
                .Select(p => new { p.Id, p.Name, p.IsDefault })
                .ToListAsync();

            var contributions = await GetInvestmentValueContributionsAsync(userId, environment: null, referenceAssetId, assetTypeId: 0, considerStable: true);

            var valueByPortfolio = contributions
                .GroupBy(c => c.PortfolioId)
                .ToDictionary(g => g.Key, g => new
                {
                    OriginalValue = g.Sum(c => c.OriginalValueContribution),
                    ActualValue = g.Sum(c => c.ActualValueContribution)
                });

            return portfolios
                .Select(p =>
                {
                    valueByPortfolio.TryGetValue(p.Id, out var value);
                    return new PortfolioStatsResult
                    {
                        PortfolioId = p.Id,
                        PortfolioName = p.Name,
                        IsDefault = p.IsDefault,
                        OriginalValue = Math.Round(value?.OriginalValue ?? 0m, 2),
                        ActualValue = Math.Round(value?.ActualValue ?? 0m, 2)
                    };
                })
                .OrderByDescending(r => r.ActualValue)
                .ToList();
        }

        // Composición y holdings dentro de una cartera puntual (ver docs/plans/activos/portfolios-estadisticas.md,
        // Fase 2). Misma granularidad activo + cuenta que ya usan GetBalance/GetAverageQuotePrice — si un
        // activo está repartido en más de una cuenta dentro de la cartera, se devuelve una fila por cuenta.
        // Filtra tenencia neta <= 0 (posiciones vendidas del todo), mismo criterio que GetStockStatsAsync.
        public async Task<IEnumerable<PortfolioHoldingResult>> GetPortfolioHoldingsAsync(int userId, int portfolioId, int referenceAssetId)
        {
            var contributions = await GetInvestmentValueContributionsAsync(
                userId, environment: null, referenceAssetId, assetTypeId: 0, considerStable: true, portfolioId: portfolioId);

            return contributions
                .GroupBy(c => new { c.AssetId, c.AccountId })
                .Select(g => new
                {
                    AssetType = g.First().AssetTypeName,
                    AssetName = g.First().AssetName,
                    g.First().Symbol,
                    AccountName = g.First().AccountName,
                    RawQuantity = g.Sum(c => c.QuantityContribution),
                    RawOriginalValue = g.Sum(c => c.OriginalValueContribution),
                    RawActualValue = g.Sum(c => c.ActualValueContribution)
                })
                .Where(x => x.RawQuantity > 0)
                .Select(x => new PortfolioHoldingResult
                {
                    AssetType = x.AssetType,
                    AssetName = x.AssetName,
                    Symbol = x.Symbol,
                    AccountName = x.AccountName,
                    Quantity = Math.Round(x.RawQuantity, 2),
                    OriginalValue = Math.Round(x.RawOriginalValue, 2),
                    ActualValue = Math.Round(x.RawActualValue, 2)
                })
                .OrderByDescending(r => r.ActualValue)
                .ToList();
        }

        // Evolución histórica del valor de una cartera, mes a mes (ver docs/plans/activos/portfolios-estadisticas.md,
        // Fase 5). A diferencia de GetInvestmentsHoldingsStats (que suma el volumen operado DENTRO de cada mes,
        // agrupado por CommerceType), esto calcula la TENENCIA ACUMULADA al cierre de cada mes — un valor de
        // patrimonio en el tiempo, no volumen de compra/venta — combinando todo tipo de activo (mismo criterio
        // "sin filtro de Environment" que GetPortfolioStatsAsync/GetPortfolioHoldingsAsync).
        // Aplica las mismas lecciones de fan-out y "última cotización por activo" que el resto de las stats:
        // tanto la cotización propia de cada activo como la de referencia se resuelven "la más reciente <= esa
        // fecha", con fan-out si hay más de una del mismo Type/fecha. Si a un activo con tenencia > 0 todavía
        // no le llegó ninguna cotización a esa altura del tiempo, ese mes no lo puede valorizar (se excluye su
        // aporte para ese mes puntual, no para los siguientes) — decisión explícita, no accidental.
        public async Task<IEnumerable<PortfolioValueByDateResult>> GetPortfolioValueByDateAsync(int userId, int portfolioId, int referenceAssetId, int months)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.PortfolioId == portfolioId)
                .Select(t => new { t.AssetId, t.Amount, t.Date })
                .ToListAsync();

            if (transactions.Count == 0)
                return Enumerable.Empty<PortfolioValueByDateResult>();

            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            var splits = await _context.AssetSplitEvents
                .Where(s => assetIds.Contains(s.AssetId))
                .Select(s => new { s.AssetId, s.Date, s.SplitRatio })
                .ToListAsync();

            decimal GetSplitFactor(int assetId, DateTime date) =>
                splits.Where(s => s.AssetId == assetId && s.Date > date).Aggregate(1m, (acc, s) => acc * s.SplitRatio);

            // Checkpoints ordenados por activo para poder calcular la tenencia acumulada a una fecha dada
            // (mismo criterio que GetCryptoStatsByDateAsync).
            var transactionsByAsset = transactions
                .GroupBy(t => t.AssetId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

            decimal GetAccumulatedQuantityAsOf(int assetId, DateTime date)
            {
                if (!transactionsByAsset.TryGetValue(assetId, out var checkpoints)) return 0m;
                return checkpoints
                    .Where(c => c.Date <= date)
                    .Sum(c => c.Amount * GetSplitFactor(assetId, c.Date));
            }

            // Historial completo de cotizaciones de los activos de la cartera, sin acotar por fecha (mismo
            // motivo que la corrección aplicada al helper de referencia: acotar por una fecha "mínima" puede
            // excluir la única cotización disponible para resolver un mes temprano si hay huecos en la carga).
            var quotesByAsset = (await _context.AssetQuotes
                    .Where(q => assetIds.Contains(q.AssetId))
                    .Select(q => new { q.AssetId, q.Date, q.Value })
                    .ToListAsync())
                .GroupBy(q => q.AssetId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(q => q.Date).ToList());

            List<decimal> GetAssetQuotesOnOrBefore(int assetId, DateTime date)
            {
                if (!quotesByAsset.TryGetValue(assetId, out var quotes)) return new List<decimal>();
                var mostRecentMatch = quotes.FirstOrDefault(q => q.Date <= date);
                if (mostRecentMatch == null) return new List<decimal>();
                return quotes.Where(q => q.Date == mostRecentMatch.Date).Select(q => q.Value).ToList();
            }

            var referenceQuotes = await _context.AssetQuotes
                .Where(q => q.AssetId == referenceAssetId && (q.Type == "BLUE" || q.Type == "NA"))
                .OrderByDescending(q => q.Date)
                .Select(q => new { q.Date, q.Value })
                .ToListAsync();

            List<decimal> GetReferenceQuotesOnOrBefore(DateTime date)
            {
                var mostRecentMatch = referenceQuotes.FirstOrDefault(q => q.Date <= date);
                if (mostRecentMatch == null) return new List<decimal>();
                return referenceQuotes.Where(q => q.Date == mostRecentMatch.Date).Select(q => q.Value).ToList();
            }

            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var startMonth = currentMonthStart.AddMonths(-(months - 1));

            var result = new List<PortfolioValueByDateResult>();
            for (var month = startMonth; month <= currentMonthStart; month = month.AddMonths(1))
            {
                var monthEnd = month.AddMonths(1).AddDays(-1);
                var referenceQuotesAtMonth = GetReferenceQuotesOnOrBefore(monthEnd);

                var monthValue = assetIds.Sum(assetId =>
                {
                    var quantity = GetAccumulatedQuantityAsOf(assetId, monthEnd);
                    if (quantity == 0) return 0m;

                    var assetQuotesAtMonth = GetAssetQuotesOnOrBefore(assetId, monthEnd);
                    if (assetQuotesAtMonth.Count == 0) return 0m; // el activo todavía no tenía cotización cargada a esa altura

                    return assetQuotesAtMonth
                        .Where(assetQuote => assetQuote > 0)
                        .SelectMany(assetQuote => referenceQuotesAtMonth.Select(refQuote => (quantity / assetQuote) * refQuote))
                        .Sum();
                });

                result.Add(new PortfolioValueByDateResult { Date = month, Value = Math.Round(monthValue, 2) });
            }

            return result;
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

            // Devuelve TODAS las cotizaciones de referencia que comparten la fecha más reciente <= la fecha
            // pedida (no solo la primera): el INNER JOIN original empareja por AssetId+Date sin TOP 1, así
            // que si esa fecha resuelta tiene más de un Type (BLUE y NA el mismo día, ej. Peso Argentino),
            // cada una genera su propia fila en el join y su propio aporte al SUM exterior (fan-out) — igual
            // que se corrigió en la Fase 5 para el mismo tipo de gap.
            List<decimal> GetReferenceQuotesOnOrBefore(DateTime date)
            {
                var mostRecentMatch = referenceQuotes.FirstOrDefault(q => q.Date <= date);
                if (mostRecentMatch == null) return new List<decimal>();
                return referenceQuotes.Where(q => q.Date == mostRecentMatch.Date).Select(q => q.Value).ToList();
            }

            var monthlyContributions = rows
                .SelectMany(r =>
                {
                    var matchingReferenceQuotes = GetReferenceQuotesOnOrBefore(r.Date);
                    if (matchingReferenceQuotes.Count == 0) return Enumerable.Empty<(DateTime Month, string CommerceType, decimal Value)>(); // INNER JOIN: sin cotización de referencia -> se excluye
                    if (!r.QuotePrice.HasValue || r.QuotePrice.Value == 0) return Enumerable.Empty<(DateTime Month, string CommerceType, decimal Value)>();

                    var isZeroedStableTrading = r.CommerceType == "Trading" && stableSymbols.Contains(r.Symbol);
                    var month = new DateTime(r.Date.Year, r.Date.Month, 1);

                    return matchingReferenceQuotes.Select(referenceQuote =>
                    {
                        var value = isZeroedStableTrading ? 0m : r.Amount * (1m / r.QuotePrice.Value) * referenceQuote;
                        return (Month: month, r.CommerceType, Value: value);
                    });
                })
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

        public async Task<IEnumerable<Transaction>> GetTransactionsByTripIdAsync(int tripId)
        {
            return await _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.TransactionClass)
                .Where(t => t.TripId == tripId)
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        // Egresos propios de un viaje (docs/plans/activos/plan-viajes-historicos.md, D1/D2): lo etiquetado con
        // TripId que NO está ya representado por el neto de los Eventos vinculados, ni como respaldo de un
        // movimiento ni como transacción que creó el motor de pagos al saldar una deuda. Esas liquidaciones
        // siguen etiquetadas a propósito (son movimientos reales de las cuentas y tienen sentido en la lista
        // del viaje): la exclusión vive acá, en el reporte, para que un etiquetado de más no rompa el total.
        public async Task<IEnumerable<Transaction>> GetTripOwnExpenseTransactionsAsync(int tripId)
        {
            var eventIds = _context.SharedEvents.Where(e => e.TripId == tripId).Select(e => e.Id);

            var backingIds = _context.SharedEventMovements
                .Where(m => eventIds.Contains(m.SharedEventId) && m.TransactionId != null)
                .Select(m => m.TransactionId.Value);

            var allocations = _context.SharedEventPaymentAllocations
                .Where(a => eventIds.Contains(a.SharedEventPayment.SharedEventId));

            var createdIds = allocations.Where(a => a.CreatedExpenseTransactionId != null).Select(a => a.CreatedExpenseTransactionId.Value)
                .Concat(allocations.Where(a => a.CreatedIncomeTransactionId != null).Select(a => a.CreatedIncomeTransactionId.Value))
                .Concat(allocations.Where(a => a.CreatedExchangeOutTransactionId != null).Select(a => a.CreatedExchangeOutTransactionId.Value))
                .Concat(allocations.Where(a => a.CreatedExchangeInTransactionId != null).Select(a => a.CreatedExchangeInTransactionId.Value));

            return await _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.TransactionClass)
                .Where(t => t.TripId == tripId && t.MovementType == "E")
                .Where(t => !backingIds.Contains(t.Id) && !createdIds.Contains(t.Id))
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        // Libera la FK para que el reintegro se pueda consolidar dentro de la cuota que lo absorbe, dejando
        // marcado que ese placeholder ya no existe: el pago del evento pasa a requerir un ajuste para revertirse.
        public async Task DetachConsumedIncomeFromSharedEventPaymentAllocationsAsync(int transactionId)
        {
            var allocations = await _context.SharedEventPaymentAllocations
                .Where(a => a.CreatedIncomeTransactionId == transactionId)
                .ToListAsync();

            foreach (var allocation in allocations)
            {
                allocation.CreatedIncomeTransactionId = null;
                allocation.IncomeTransactionConsumed = true;
                allocation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTripSuggestibleTransactionsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var endExclusive = endDate.Date.AddDays(1);
            return await _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.TransactionClass)
                .Where(t => t.UserId == userId
                    && t.TripId == null
                    && t.MovementType == "E"
                    && t.CardTransactionId == null
                    && t.TransactionClassId != null
                    && !TripMovementRules.ExcludedTransactionClasses.Contains(t.TransactionClass.Description)
                    && (t.Detail == null || !t.Detail.StartsWith(TripMovementRules.LegacyCardPaymentDetailPrefix))
                    && t.Date >= startDate.Date
                    && t.Date < endExclusive)
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> SearchTripAssociableTransactionsAsync(int userId, string? search)
        {
            var query = _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.TransactionClass)
                .Where(t => t.UserId == userId
                    && t.TripId == null
                    && t.MovementType == "E"
                    && t.CardTransactionId == null
                    && t.TransactionClassId != null
                    && !TripMovementRules.ExcludedTransactionClasses.Contains(t.TransactionClass.Description)
                    && (t.Detail == null || !t.Detail.StartsWith(TripMovementRules.LegacyCardPaymentDetailPrefix)));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Detail != null && t.Detail.Contains(search));

            return await query
                .OrderByDescending(t => t.Date)
                .Take(50)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByCardTransactionIdAsync(int cardTransactionId)
        {
            return await _context.Transactions
                .Where(t => t.CardTransactionId == cardTransactionId)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToListAsync();
        }
    }
}
