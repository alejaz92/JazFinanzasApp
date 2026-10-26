using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.DTO.PurchaseSpending;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Las cuentas de agregar, comparar y rankear, como funciones puras sobre filas ya convertidas: es lo que prueban los tests sin
    // base de datos.
    public static class PurchaseSpendingCalculator
    {
        // Gasto por categoría del tramo "del 1 al día N" de cada mes. Cada mes se acota al mismo tramo, o a su propia cantidad de días
        // si es más corto (febrero), igual que el termómetro de Inicio y que GetSpendingToDateByCategoryAsync.
        public static SpendingToDateDTO ToDateByCategory(IReadOnlyCollection<PurchaseSpendingRowResult> rows, int assetId, DateTime month, int cutoffDay, int comparisonMonths)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);

            decimal SumToDate(IEnumerable<PurchaseSpendingRowResult> categoryRows, DateTime bucketStart)
            {
                var days = Math.Min(cutoffDay, DateTime.DaysInMonth(bucketStart.Year, bucketStart.Month));
                var bucketEnd = bucketStart.AddDays(days);
                return categoryRows.Where(r => r.Date >= bucketStart && r.Date < bucketEnd).Sum(r => r.AmountByAssetId[assetId]);
            }

            var categories = new List<SpendingToDateCategoryDTO>();
            foreach (var group in rows.GroupBy(r => new { r.CategoryId, r.CategoryName, r.ParentName }))
            {
                var averageSum = 0m;
                for (var i = 1; i <= comparisonMonths; i++)
                    averageSum += SumToDate(group, monthStart.AddMonths(-i));

                var category = new SpendingToDateCategoryDTO
                {
                    CategoryId = group.Key.CategoryId,
                    CategoryName = group.Key.CategoryName,
                    ParentName = group.Key.ParentName,
                    CurrentToDate = Math.Round(SumToDate(group, monthStart), 2),
                    PreviousToDate = Math.Round(SumToDate(group, monthStart.AddMonths(-1)), 2),
                    AverageToDate = Math.Round(averageSum / comparisonMonths, 2)
                };
                if (category.CurrentToDate != 0m || category.PreviousToDate != 0m || category.AverageToDate != 0m)
                    categories.Add(category);
            }

            return new SpendingToDateDTO
            {
                Month = monthStart,
                CutoffDay = cutoffDay,
                ComparisonMonths = comparisonMonths,
                // Los totales son la suma de las categorías ya redondeadas: así cierran exactamente.
                TotalCurrent = categories.Sum(c => c.CurrentToDate),
                TotalPrevious = categories.Sum(c => c.PreviousToDate),
                TotalAverage = categories.Sum(c => c.AverageToDate),
                Categories = categories.OrderByDescending(c => Math.Max(c.CurrentToDate, c.AverageToDate)).ThenBy(c => c.CategoryName).ToList()
            };
        }

        public static List<DaySpendingDTO> DailySeries(IEnumerable<PurchaseSpendingRowResult> rows, int assetId) =>
            rows.GroupBy(r => r.Date.Date)
                .Select(g => new DaySpendingDTO { Date = g.Key, Amount = Math.Round(g.Sum(r => r.AmountByAssetId[assetId]), 2) })
                .OrderBy(d => d.Date)
                .ToList();

        // Los recurrentes se cobran todos los meses: no son un movimiento puntual que ranquear.
        public static List<PurchaseSpendingRowResult> Largest(IEnumerable<PurchaseSpendingRowResult> rows, int rankingAssetId, int take, int? categoryId) =>
            rows.Where(r => r.Kind != PurchaseSpendingKind.CardRecurring && (categoryId == null || r.CategoryId == categoryId))
                .OrderByDescending(r => r.AmountByAssetId[rankingAssetId])
                .ThenBy(r => r.Kind)
                .ThenBy(r => r.SourceId)
                .Take(take)
                .ToList();

        public static InstallmentsDueDTO InstallmentsDue(IEnumerable<CardInstallmentDueResult> due, DateTime month)
        {
            var list = due.ToList();
            var totalInstallments = list.Where(d => !d.IsRecurring).Sum(d => d.Amount);
            var totalRecurring = list.Where(d => d.IsRecurring).Sum(d => d.Amount);
            return new InstallmentsDueDTO
            {
                Month = new DateTime(month.Year, month.Month, 1),
                TotalInstallments = Math.Round(totalInstallments, 2),
                TotalRecurring = Math.Round(totalRecurring, 2),
                Total = Math.Round(totalInstallments + totalRecurring, 2),
                Categories = list.GroupBy(d => new { d.CategoryId, d.CategoryName })
                    .Select(g => new InstallmentsDueCategoryDTO { CategoryId = g.Key.CategoryId, CategoryName = g.Key.CategoryName, Amount = Math.Round(g.Sum(d => d.Amount), 2) })
                    .OrderByDescending(c => c.Amount).ThenBy(c => c.CategoryName)
                    .ToList()
            };
        }
    }

    public class PurchaseSpendingService : IPurchaseSpendingService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;

        // Las filas ya traídas en este pedido, por usuario y monedas. El servicio es scoped (vive lo que dura un pedido o una
        // pasada del asistente), así que las tres fuentes que lo usan comparten una sola consulta: si lo pedido cae dentro de un
        // rango ya traído, se filtra en memoria. Los datos no pueden cambiar en medio de la generación de una devolución.
        private readonly Dictionary<string, (DateTime From, DateTime To, List<PurchaseSpendingRowResult> Rows)> _rowsCache = new();

        public PurchaseSpendingService(ITransactionRepository transactionRepository, IAssetRepository assetRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
        }

        private async Task<List<PurchaseSpendingRowResult>> GetRowsAsync(int userId, IReadOnlyList<Asset> assets, DateTime from, DateTime toExclusive)
        {
            var key = $"{userId}|{string.Join(',', assets.Select(a => a.Id).OrderBy(id => id))}";
            if (_rowsCache.TryGetValue(key, out var cached) && cached.From <= from && cached.To >= toExclusive)
                return cached.Rows.Where(r => r.Date >= from && r.Date < toExclusive).ToList();

            var rows = (await _transactionRepository.GetPurchaseSpendingRowsAsync(userId, assets, from, toExclusive)).ToList();
            // Se guarda el rango más ancho: lo que se pida después y quepa adentro no vuelve a la base.
            if (!_rowsCache.TryGetValue(key, out var existing) || (from <= existing.From && toExclusive >= existing.To))
                _rowsCache[key] = (from, toExclusive, rows);
            return rows;
        }

        public async Task<SpendingToDateDTO> GetToDateByCategoryAsync(int userId, int assetId, DateTime month, int cutoffDay, int comparisonMonths = 6)
        {
            if (cutoffDay < 1)
                throw new BusinessRuleException("El día de corte tiene que ser 1 o mayor.");
            if (comparisonMonths < 1)
                throw new BusinessRuleException("Hay que comparar contra al menos un mes anterior.");

            var asset = await GetAssetAsync(assetId);
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var effectiveCutoff = Math.Min(cutoffDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

            var rows = await GetRowsAsync(userId, new[] { asset }, monthStart.AddMonths(-comparisonMonths), monthStart.AddMonths(1));

            return PurchaseSpendingCalculator.ToDateByCategory(rows, asset.Id, monthStart, effectiveCutoff, comparisonMonths);
        }

        public async Task<List<DaySpendingDTO>> GetDailySeriesAsync(int userId, int assetId, DateTime from, DateTime toExclusive)
        {
            if (toExclusive <= from)
                throw new BusinessRuleException("El rango de fechas no es válido.");

            var asset = await GetAssetAsync(assetId);
            var rows = await GetRowsAsync(userId, new[] { asset }, from, toExclusive);
            return PurchaseSpendingCalculator.DailySeries(rows, asset.Id);
        }

        public async Task<List<PurchaseSpendingRowResult>> GetLargestAsync(int userId, IReadOnlyList<int> assetIds, DateTime from, DateTime toExclusive, int take, int? categoryId = null)
        {
            if (assetIds.Count == 0)
                throw new BusinessRuleException("Se necesita al menos una moneda.");
            if (take < 1)
                throw new BusinessRuleException("Hay que pedir al menos un movimiento.");

            var assets = new List<Asset>();
            foreach (var id in assetIds)
                assets.Add(await GetAssetAsync(id));

            var rows = await GetRowsAsync(userId, assets, from, toExclusive);
            return PurchaseSpendingCalculator.Largest(rows, assets[0].Id, take, categoryId);
        }

        public async Task<IReadOnlyDictionary<int, PurchaseSpendingWindowDTO>> GetWindowsAsync(int userId, IReadOnlyList<int> assetIds, DateTime month, int cutoffDay, int comparisonMonths = 6)
        {
            if (assetIds.Count == 0)
                throw new BusinessRuleException("Se necesita al menos una moneda.");
            if (cutoffDay < 1)
                throw new BusinessRuleException("El día de corte tiene que ser 1 o mayor.");
            if (comparisonMonths < 1)
                throw new BusinessRuleException("Hay que comparar contra al menos un mes anterior.");

            var assets = new List<Asset>();
            foreach (var id in assetIds)
                assets.Add(await GetAssetAsync(id));

            var monthStart = new DateTime(month.Year, month.Month, 1);
            var effectiveCutoff = Math.Min(cutoffDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

            // Una sola consulta para todas las monedas: cada fila trae su monto en cada una.
            var rows = await GetRowsAsync(userId, assets, monthStart.AddMonths(-comparisonMonths), monthStart.AddMonths(1));
            var daysFrom = monthStart.AddMonths(-1);

            var result = new Dictionary<int, PurchaseSpendingWindowDTO>();
            foreach (var asset in assets)
            {
                result[asset.Id] = new PurchaseSpendingWindowDTO
                {
                    ToDate = PurchaseSpendingCalculator.ToDateByCategory(rows, asset.Id, monthStart, effectiveCutoff, comparisonMonths),
                    FullMonths = PurchaseSpendingCalculator.ToDateByCategory(rows, asset.Id, monthStart, 31, comparisonMonths),
                    Days = PurchaseSpendingCalculator.DailySeries(rows.Where(r => r.Date >= daysFrom), asset.Id)
                };
            }
            return result;
        }

        public async Task<InstallmentsDueDTO> GetInstallmentsDueAsync(int userId, int assetId, DateTime month)
        {
            var asset = await GetAssetAsync(assetId);
            var due = await _transactionRepository.GetCardInstallmentsDueAsync(userId, asset, month);
            return PurchaseSpendingCalculator.InstallmentsDue(due, month);
        }

        private async Task<Asset> GetAssetAsync(int assetId) =>
            await _assetRepository.GetByIdAsync(assetId) ?? throw new NotFoundException("Asset not found");
    }
}
