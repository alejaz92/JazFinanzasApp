using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class IncomeExpenseReportService : IIncomeExpenseReportService
    {
        // D-A: ventana móvil de 6 meses para las comparaciones — los 5 meses de arranque (marzo-julio
        // 2024) no ensucian ningún promedio (1.1 del plan).
        private const int MovingAverageWindowMonths = 6;
        private const int CategoryTrendMonths = 6;
        private const int DefaultEvolutionMonths = 24;
        private const int DefaultTagMonths = 6;
        private const int DefaultPayDayMonths = 12;

        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;

        public IncomeExpenseReportService(ITransactionRepository transactionRepository, IAssetRepository assetRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
        }

        public async Task<IncExpWaterfallDTO> GetWaterfallAsync(int userId, DateTime month, int assetId)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var result = await _transactionRepository.GetIncExpWaterfallAsync(userId, month, asset);

            return new IncExpWaterfallDTO
            {
                Month = result.Month,
                TotalIncome = result.TotalIncome,
                ExpenseSteps = result.ExpenseSteps.Select(s => new WaterfallStepDTO { CategoryName = s.CategoryName, Amount = s.Amount }).ToList(),
                TotalExpense = result.TotalExpense,
                Result = result.Result,
                PreviousMonthResult = result.PreviousMonthResult
            };
        }

        public async Task<IEnumerable<IncExpEvolutionPointDTO>> GetEvolutionAsync(int userId, int assetId, int months = DefaultEvolutionMonths)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var points = (await _transactionRepository.GetIncExpEvolutionAsync(userId, asset, months)).ToList();
            return BuildEvolutionDTO(points);
        }

        public async Task<SpendingByCategoryDTO> GetByCategoryAsync(int userId, DateTime month, int assetId)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var categories = (await _transactionRepository.GetSpendingByCategoryMonthlySeriesAsync(userId, asset, month, CategoryTrendMonths)).ToList();
            var monthStart = new DateTime(month.Year, month.Month, 1);
            return BuildSpendingByCategoryDTO(monthStart, categories);
        }

        public async Task<IEnumerable<TagSpendingDTO>> GetByTagAsync(int userId, int assetId, int months = DefaultTagMonths)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var tags = await _transactionRepository.GetSpendingByTagAsync(userId, asset, months);

            return tags.Select(t => new TagSpendingDTO
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Color = t.Color,
                TotalAmount = t.TotalAmount,
                MonthlyEvolution = t.MonthlyEvolution.Select(m => new MonthlyAmountDTO { Month = m.Month, Amount = m.Amount }).ToList(),
                ByCategory = t.ByCategory.Select(c => new CategoryAmountDTO { CategoryName = c.CategoryName, Amount = c.Amount }).ToList()
            });
        }

        public async Task<SpendingCalendarDTO> GetCalendarAsync(int userId, int assetId, int year)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var days = (await _transactionRepository.GetDailySpendingAsync(userId, asset, year)).ToList();
            return BuildCalendarDTO(year, days);
        }

        // Ingresos (corrección 2026-09-04 sobre la Fase 13): evolución por categoría en el tiempo.
        public async Task<IEnumerable<IncomeCategorySeriesDTO>> GetIncomeByCategoryAsync(int userId, int assetId, int months = DefaultEvolutionMonths)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var series = await _transactionRepository.GetIncomeByCategoryMonthlySeriesAsync(userId, asset, months);
            return series.Select(s => new IncomeCategorySeriesDTO { CategoryId = s.CategoryId, CategoryName = s.CategoryName, MonthlyTrend = s.MonthlyTrend });
        }

        public async Task<PayDayCalendarDTO> GetPayDaysAsync(int userId, int assetId, int months = DefaultPayDayMonths)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var days = (await _transactionRepository.GetDailyIncomeAsync(userId, asset, months)).ToList();
            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            return BuildPayDayCalendarDTO(currentMonthStart, months, days);
        }

        // Pura — testeable sin mocks. Un día "recibido" es un día con ingreso > 0; el promedio se
        // calcula solo sobre esos días (no se diluye con los meses en que ese día no cobró nada) y
        // la frecuencia (ver PayDayDTO.FrequencyPct) es la que dice si es un día de cobro habitual
        // o un ingreso ocasional grande.
        public static PayDayCalendarDTO BuildPayDayCalendarDTO(DateTime currentMonthStart, int months, List<DailySpendingResult> days)
        {
            var start = currentMonthStart.AddMonths(-(months - 1));

            var monthsInWindowByDay = new int[32];
            for (var m = start; m <= currentMonthStart; m = m.AddMonths(1))
            {
                var daysInMonth = DateTime.DaysInMonth(m.Year, m.Month);
                for (int d = 1; d <= daysInMonth; d++) monthsInWindowByDay[d]++;
            }

            var totalsByDay = new decimal[32];
            var countsByDay = new int[32];
            foreach (var d in days)
            {
                if (d.Amount <= 0) continue;
                var day = d.Date.Day;
                totalsByDay[day] += d.Amount;
                countsByDay[day]++;
            }

            var result = new PayDayCalendarDTO();
            for (int day = 1; day <= 31; day++)
            {
                if (monthsInWindowByDay[day] == 0) continue;
                result.Days.Add(new PayDayDTO
                {
                    Day = day,
                    AverageAmountWhenReceived = countsByDay[day] > 0 ? Math.Round(totalsByDay[day] / countsByDay[day], 2) : 0m,
                    TimesReceived = countsByDay[day],
                    MonthsInWindow = monthsInWindowByDay[day]
                });
            }
            return result;
        }

        private async Task<Asset> GetCurrencyAssetAsync(int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            if (asset.AssetTypeId != 1)
                throw new BusinessRuleException("El activo no es una moneda");
            return asset;
        }

        // Pura — testeable sin mocks. Arma el acumulado y el promedio móvil de gasto (D-A) sobre la
        // serie mensual que ya trajo el repositorio.
        public static List<IncExpEvolutionPointDTO> BuildEvolutionDTO(List<IncExpEvolutionPointResult> points)
        {
            var result = new List<IncExpEvolutionPointDTO>();
            decimal cumulative = 0m;

            for (int i = 0; i < points.Count; i++)
            {
                cumulative += points[i].Result;

                decimal? movingAverage = null;
                if (i + 1 >= MovingAverageWindowMonths)
                {
                    var window = points.Skip(i + 1 - MovingAverageWindowMonths).Take(MovingAverageWindowMonths);
                    movingAverage = Math.Round(window.Average(p => p.Expense), 2);
                }

                result.Add(new IncExpEvolutionPointDTO
                {
                    Month = points[i].Month,
                    Income = points[i].Income,
                    Expense = points[i].Expense,
                    Result = points[i].Result,
                    CumulativeResult = Math.Round(cumulative, 2),
                    ExpenseMovingAverage = movingAverage
                });
            }

            return result;
        }

        // Pura — testeable sin mocks. Agrupa por rubro (ParentId ?? CategoryId, T4) y calcula el
        // ranking del mes pedido contra el anterior (D-2 / "cómo se reordenaron mes a mes").
        public static SpendingByCategoryDTO BuildSpendingByCategoryDTO(DateTime month, List<CategorySpendingResult> categories)
        {
            var dto = new SpendingByCategoryDTO { Month = month };
            if (categories.Count == 0) return dto;

            int lastIndex = categories[0].MonthlyTrend.Count - 1;
            int prevIndex = lastIndex - 1;

            var rankCurrent = categories
                .Select(c => (c.CategoryId, Amount: c.MonthlyTrend[lastIndex]))
                .OrderByDescending(x => x.Amount)
                .Select((x, i) => (x.CategoryId, Rank: i + 1))
                .ToDictionary(x => x.CategoryId, x => x.Rank);

            Dictionary<int, int>? rankPrevious = prevIndex < 0 ? null : categories
                .Select(c => (c.CategoryId, Amount: c.MonthlyTrend[prevIndex]))
                .OrderByDescending(x => x.Amount)
                .Select((x, i) => (x.CategoryId, Rank: i + 1))
                .ToDictionary(x => x.CategoryId, x => x.Rank);

            dto.Groups = categories
                .GroupBy(c => c.ParentId ?? c.CategoryId)
                .Select(g =>
                {
                    var groupName = g.First().ParentId.HasValue ? g.First().ParentName! : g.First().CategoryName;
                    return new CategoryGroupDTO
                    {
                        GroupId = g.Key,
                        GroupName = groupName,
                        Amount = Math.Round(g.Sum(c => c.MonthlyTrend[lastIndex]), 2),
                        Categories = g.Select(c => new CategoryDetailDTO
                        {
                            CategoryId = c.CategoryId,
                            CategoryName = c.CategoryName,
                            Amount = Math.Round(c.MonthlyTrend[lastIndex], 2),
                            MonthlyTrend = c.MonthlyTrend,
                            RankCurrent = rankCurrent[c.CategoryId],
                            RankPrevious = rankPrevious != null && rankPrevious.TryGetValue(c.CategoryId, out var rp) ? rp : null
                        }).OrderByDescending(c => c.Amount).ToList()
                    };
                })
                .OrderByDescending(g => g.Amount)
                .ToList();

            return dto;
        }

        // Pura — testeable sin mocks. El promedio por día de semana divide por la cantidad de veces
        // que ese día ocurrió en el rango (incluye los días sin gasto), no solo por los días con datos.
        public static SpendingCalendarDTO BuildCalendarDTO(int year, List<DailySpendingResult> days)
        {
            var yearStart = new DateTime(year, 1, 1);
            var rangeEnd = new DateTime(year, 12, 31) < DateTime.Today ? new DateTime(year + 1, 1, 1) : DateTime.Today.AddDays(1);
            var amountByDate = days.ToDictionary(d => d.Date, d => d.Amount);

            var weekdayTotals = new decimal[7];
            var weekdayCounts = new int[7];
            for (var d = yearStart; d < rangeEnd; d = d.AddDays(1))
            {
                var dow = (int)d.DayOfWeek;
                weekdayCounts[dow]++;
                weekdayTotals[dow] += amountByDate.TryGetValue(d, out var amt) ? amt : 0m;
            }

            var weekdayAverages = Enumerable.Range(0, 7)
                .Select(i => new WeekdayAverageDTO
                {
                    DayOfWeek = (DayOfWeek)i,
                    Average = weekdayCounts[i] > 0 ? Math.Round(weekdayTotals[i] / weekdayCounts[i], 2) : 0m
                })
                .ToList();

            return new SpendingCalendarDTO
            {
                Year = year,
                Days = days.Select(d => new DaySpendingDTO { Date = d.Date, Amount = d.Amount }).ToList(),
                WeekdayAverages = weekdayAverages
            };
        }
    }
}
