using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.DTO.Dashboard;
using JazFinanzasApp.API.Business.DTO.IncomeExpenseReport;
using JazFinanzasApp.API.Business.DTO.NetWorth;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Fase 16: compone los servicios de los bloques anteriores (Patrimonio, Ingresos y Egresos,
    // Tarjetas, Compartidos, Viajes) en un solo endpoint para Inicio y el Panorama — nada de esta
    // clase recalcula un número que ya sabe calcular otro servicio.
    public class DashboardService : IDashboardService
    {
        private const string CurrencyAssetTypeName = "Moneda";

        // Mismo umbral que el aviso de vencimiento de tarjeta del frontend
        // (features/card/utils/card-due-status.util.ts, ALERT_THRESHOLD_DAYS) — se porta acá para que
        // la bandeja de pendientes decida con el mismo criterio, sin duplicar un segundo número.
        private const int CardAlertThresholdDays = 3;

        // "Varios días" (sección 5.3) sin gastos cargados en un viaje en curso: mismo umbral que la
        // alerta de tarjeta de arriba, para no sumarle a la app un tercer criterio de "pronto".
        private const int TripStaleDaysThreshold = 3;

        private readonly INetWorthReportService _netWorthReportService;
        private readonly IIncomeExpenseReportService _incomeExpenseReportService;
        private readonly ICardReportService _cardReportService;
        private readonly ICardService _cardService;
        private readonly ISharedEventService _sharedEventService;
        private readonly ISharedExpenseService _sharedExpenseService;
        private readonly ITripService _tripService;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public DashboardService(
            INetWorthReportService netWorthReportService,
            IIncomeExpenseReportService incomeExpenseReportService,
            ICardReportService cardReportService,
            ICardService cardService,
            ISharedEventService sharedEventService,
            ISharedExpenseService sharedExpenseService,
            ITripService tripService,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository)
        {
            _netWorthReportService = netWorthReportService;
            _incomeExpenseReportService = incomeExpenseReportService;
            _cardReportService = cardReportService;
            _cardService = cardService;
            _sharedEventService = sharedEventService;
            _sharedExpenseService = sharedExpenseService;
            _tripService = tripService;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
        }

        public async Task<DashboardDTO> GetDashboardAsync(int userId, int assetId)
        {
            var asset = await GetCurrencyAssetAsync(assetId);
            var today = DateTime.Today;
            // plan-alerta-cotizaciones, T3: se pide una sola vez y se comparte entre Indicadores
            // (ya lo usaba) y Pendientes (aviso de cotizaciones nuevo) — así los dos no pueden
            // divergir, y cada usuario ve solo lo suyo porque GetGeneralAsync ya es por usuario.
            var netWorthGeneral = await _netWorthReportService.GetGeneralAsync(userId);

            return new DashboardDTO
            {
                Indicators = await BuildIndicatorsAsync(userId, asset, today, netWorthGeneral),
                Thermometer = await BuildThermometerAsync(userId, asset, today),
                Pending = await BuildPendingAsync(userId, asset, today, netWorthGeneral)
            };
        }

        private async Task<DashboardIndicatorsDTO> BuildIndicatorsAsync(int userId, Asset asset, DateTime today, NetWorthGeneralDTO netWorthGeneral)
        {
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            var accounts = await _netWorthReportService.GetByAccountAsync(userId, asset.Id);
            var available = Math.Round(accounts
                .SelectMany(a => a.Holdings)
                .Where(h => h.AssetTypeName == CurrencyAssetTypeName)
                .Sum(h => h.BalanceInReferenceAsset), 2);

            var total = netWorthGeneral.Totals.FirstOrDefault(t => t.Asset == asset.Name);

            var monthlySeries = (await _netWorthReportService.GetMonthlySeriesAsync(userId, asset.Id)).ToList();
            var netWorthChange = monthlySeries.Count >= 2
                ? Math.Round(monthlySeries[^1].Total - monthlySeries[^2].Total, 2)
                : 0m;

            var waterfall = await _incomeExpenseReportService.GetWaterfallAsync(userId, currentMonth, asset.Id);

            var monthSummary = await _cardReportService.GetMonthSummaryAsync(userId, currentMonth);
            var cards = await _cardService.GetAllForUserAsync(userId);
            var (cardsDueAmount, cardsNextDueDate) = BuildCardsDue(monthSummary, cards);

            var debts = await _sharedEventService.GetConsolidatedDebtsAsync(userId);

            return new DashboardIndicatorsDTO
            {
                ReferenceAssetSymbol = asset.Symbol,
                Available = available,
                NetWorthGross = total?.GrossBalance ?? 0m,
                NetWorthNet = total?.NetBalance ?? 0m,
                NetWorthChangeVsPreviousMonth = netWorthChange,
                MonthResult = waterfall.Result,
                MonthResultPreviousMonth = waterfall.PreviousMonthResult,
                CardsDueAmountInPesos = cardsDueAmount,
                CardsNextDueDate = cardsNextDueDate,
                SharedBalances = BuildSharedBalances(debts)
            };
        }

        private async Task<DashboardThermometerDTO> BuildThermometerAsync(int userId, Asset asset, DateTime today)
        {
            var previousMonthYear = new DateTime(today.Year, today.Month, 1).AddMonths(-1).Year;

            var days = (await _incomeExpenseReportService.GetCalendarAsync(userId, asset.Id, today.Year)).Days.ToList();
            if (previousMonthYear != today.Year)
            {
                var previousYearDays = (await _incomeExpenseReportService.GetCalendarAsync(userId, asset.Id, previousMonthYear)).Days;
                days.AddRange(previousYearDays);
            }

            return BuildThermometer(days, today);
        }

        private async Task<List<DashboardPendingItemDTO>> BuildPendingAsync(int userId, Asset asset, DateTime today, NetWorthGeneralDTO netWorthGeneral)
        {
            var pending = new List<DashboardPendingItemDTO>();

            var cards = await _cardService.GetAllForUserAsync(userId);
            foreach (var card in cards)
            {
                var status = GetCardDueStatus(card, today);
                if (status != "alerta" && status != "vencido") continue;

                pending.Add(new DashboardPendingItemDTO
                {
                    Kind = "CardDue",
                    Title = card.Name,
                    Detail = status == "vencido" ? "Vencida" : "Vence pronto",
                    Date = card.NextDueDate,
                    LinkId = card.Id,
                    Severity = status == "vencido" ? "danger" : "warning"
                });
            }

            // Corrección 2026-09-08: cuánto ("te deben"/"debés" según el signo de MyBalance), no solo
            // el nombre del evento — mismo dato que ya mostraba el HomeComponent viejo. Un evento con
            // más de una moneda con saldo (raro — no se vio ningún caso en 1.6/demo) muestra la
            // primera; DashboardPendingItemDTO tiene un solo Amount/AssetSymbol, no una lista.
            var activeSummaries = await _sharedEventService.GetActiveSummaryAsync(userId);
            foreach (var summary in activeSummaries.Where(s => s.Balances.Any(b => Math.Abs(b.MyBalance) > 0.01m)))
            {
                var balance = summary.Balances.First(b => Math.Abs(b.MyBalance) > 0.01m);

                pending.Add(new DashboardPendingItemDTO
                {
                    Kind = "OpenSharedEvent",
                    Title = summary.Name,
                    Detail = balance.MyBalance > 0 ? "Te deben" : "Debés",
                    Amount = Math.Abs(balance.MyBalance),
                    AssetSymbol = balance.AssetSymbol,
                    LinkId = summary.EventId
                });
            }

            // Gastos sueltos (SharedExpense V1, sin Evento): la misma deuda que ya suma
            // GetConsolidatedDebtsAsync para "Saldo compartido" arriba, pero acá hace falta el
            // desglose por persona — es "cualquier otra deuda relacionada conmigo" (pedido del
            // usuario), no solo la de los Eventos formales. Siempre en la dirección "te debe" (el
            // usuario pagó, el otro tiene pendiente su parte) — GetSummaryAsync no devuelve el caso
            // contrario. Sin moneda explícita: ni el DTO ni la pantalla "Gastos sueltos pendientes"
            // (que "Ver deuda" abre) la muestran, mismo criterio.
            var personDebts = await _sharedExpenseService.GetSummaryAsync(userId);
            foreach (var debt in personDebts.Where(d => d.TotalPending > 0.01m))
            {
                pending.Add(new DashboardPendingItemDTO
                {
                    Kind = "PersonDebt",
                    Title = debt.PersonName,
                    Detail = "Te debe",
                    Amount = debt.TotalPending,
                    LinkId = debt.PersonId
                });
            }

            var trips = await _tripService.GetAllForUserAsync(userId);
            foreach (var trip in trips.Where(t => t.Status == "IN_PROGRESS"))
            {
                var detail = await _tripService.GetByIdAsync(userId, trip.Id);
                var item = GetTripPendingItem(detail, today, TripStaleDaysThreshold);
                if (item != null) pending.Add(item);
            }

            // plan-alerta-cotizaciones: `netWorthGeneral.StaleAssets` ya viene filtrado por
            // NetWorthReportService.StaleDaysThreshold (T2) a lo que el usuario tiene en cartera —
            // acá solo se agrupa por familia (T4) y se decide severidad (T1), sin pedir nada más al
            // repositorio de transacciones.
            var latestQuoteDateByAssetType = await _assetQuoteRepository.GetLatestQuoteDateByAssetTypeAsync();
            pending.AddRange(BuildStaleQuotePendingItems(netWorthGeneral.StaleAssets, latestQuoteDateByAssetType, today));

            return pending;
        }

        // Pura — testeable sin mocks. Suma ValueInPesos (Tarjetas → General ya lo muestra sin
        // convertir, mismo criterio acá) y toma el vencimiento más próximo entre las tarjetas con algo
        // por pagar este mes.
        public static (decimal AmountInPesos, DateTime? NextDueDate) BuildCardsDue(
            IEnumerable<Business.DTO.CardTransaction.CardTransactionPaymentListDTO> monthSummary,
            IEnumerable<CardDTO> cards)
        {
            var summaryList = monthSummary.ToList();
            var amount = Math.Round(summaryList.Sum(m => m.ValueInPesos), 2);

            var relevantCardIds = summaryList.Select(m => m.CardId).ToHashSet();
            var dueDates = cards
                .Where(c => relevantCardIds.Contains(c.Id) && c.NextDueDate.HasValue)
                .Select(c => c.NextDueDate!.Value)
                .ToList();

            return (amount, dueDates.Count > 0 ? dueDates.Min() : null);
        }

        // Pura — testeable sin mocks. Mismo cálculo que HomeComponent.computeConsolidatedTotals
        // (frontend, hoy) llevado al backend: neto por moneda sobre todos los eventos y deudas
        // sueltas, filtrando lo que redondea a cero.
        public static List<DashboardSharedBalanceDTO> BuildSharedBalances(IEnumerable<SharedEventConsolidatedDebtDTO> debts)
        {
            var totals = new Dictionary<int, DashboardSharedBalanceDTO>();
            foreach (var d in debts)
            {
                var net = d.PendingInFavor - d.PendingAgainst;
                if (totals.TryGetValue(d.AssetId, out var existing))
                    existing.Net += net;
                else
                    totals[d.AssetId] = new DashboardSharedBalanceDTO { AssetId = d.AssetId, AssetSymbol = d.AssetSymbol, Net = net };
            }

            return totals.Values.Where(t => Math.Abs(t.Net) > 0.01m).ToList();
        }

        // Pura — testeable sin mocks. Puerto de card-due-status.util.ts (frontend) al backend: el
        // aviso solo tiene sentido en la ventana cierre → vencimiento (antes del cierre no hay
        // resumen generado; pagado el período, el aviso desaparece).
        public static string GetCardDueStatus(CardDTO card, DateTime today)
        {
            if (!card.NextClosingDate.HasValue || !card.NextDueDate.HasValue) return "ninguno";
            if (card.IsCurrentPeriodPaid) return "ninguno";

            var closing = card.NextClosingDate.Value.Date;
            var due = card.NextDueDate.Value.Date;
            var todayDate = today.Date;

            if (todayDate < closing) return "ninguno";
            if (todayDate > due) return "vencido";

            var daysUntilDue = (due - todayDate).Days;
            return daysUntilDue <= CardAlertThresholdDays ? "alerta" : "informativo";
        }

        // Pura — testeable sin mocks. Un viaje sin ningún movimiento todavía siempre entra (es
        // exactamente el caso que la bandeja tiene que avisar); con movimientos, entra si pasaron
        // `staleDaysThreshold` días o más desde el último.
        public static DashboardPendingItemDTO? GetTripPendingItem(TripDetailDTO trip, DateTime today, int staleDaysThreshold)
        {
            if (trip.Movements.Count == 0)
            {
                return new DashboardPendingItemDTO
                {
                    Kind = "TripWithoutRecentExpense",
                    Title = trip.Name,
                    Detail = "Sin gastos cargados",
                    LinkId = trip.Id
                };
            }

            var lastMovementDate = trip.Movements.Max(m => m.Date).Date;
            var daysSinceLastExpense = (today.Date - lastMovementDate).Days;
            if (daysSinceLastExpense < staleDaysThreshold) return null;

            return new DashboardPendingItemDTO
            {
                Kind = "TripWithoutRecentExpense",
                Title = trip.Name,
                Detail = $"Sin gastos hace {daysSinceLastExpense} días",
                Date = lastMovementDate,
                LinkId = trip.Id
            };
        }

        // Pura — testeable sin mocks. T1: atraso = días desde la última cotización; hay corte si
        // atraso >= 2 (falta la de ayer), y pasa de "warning" a "danger" a los 5. T4: una familia
        // (AssetType) está frenada si la última cotización de TODO el catálogo de ese tipo también
        // tiene atraso >= 2 — ahí se junta en una sola fila con los símbolos que el usuario tiene en
        // esa familia; si no, cada activo suelto entra con su propia fila. `staleAssets` ya viene
        // filtrado por NetWorthReportService (solo lo que el usuario tiene hoy en cartera, D-1).
        public static List<DashboardPendingItemDTO> BuildStaleQuotePendingItems(
            IEnumerable<StaleAssetDTO> staleAssets,
            Dictionary<string, DateTime> latestQuoteDateByAssetType,
            DateTime today)
        {
            const int StaleThresholdDays = 2;
            const int DangerThresholdDays = 5;

            string Severity(int daysStale) => daysStale >= DangerThresholdDays ? "danger" : "warning";

            var items = new List<DashboardPendingItemDTO>();

            foreach (var group in staleAssets.GroupBy(a => a.AssetTypeName))
            {
                var familyLastQuote = latestQuoteDateByAssetType.TryGetValue(group.Key, out var d) ? d : (DateTime?)null;
                var familyDaysStale = familyLastQuote.HasValue ? (today - familyLastQuote.Value).Days : 0;

                if (familyLastQuote.HasValue && familyDaysStale >= StaleThresholdDays)
                {
                    var symbols = string.Join(", ", group.Select(a => a.AssetSymbol));
                    items.Add(new DashboardPendingItemDTO
                    {
                        Kind = "StaleQuote",
                        Title = group.Key,
                        Detail = $"Sin cotizar desde el {familyLastQuote.Value:dd/MM/yyyy} ({familyDaysStale} días) — {symbols}",
                        Severity = Severity(familyDaysStale)
                    });
                    continue;
                }

                foreach (var a in group)
                {
                    var daysStale = (today - a.QuoteDate).Days;
                    items.Add(new DashboardPendingItemDTO
                    {
                        Kind = "StaleQuote",
                        Title = a.AssetSymbol,
                        Detail = $"Sin cotizar desde el {a.QuoteDate:dd/MM/yyyy} ({daysStale} días)",
                        Severity = Severity(daysStale)
                    });
                }
            }

            return items;
        }

        // Pura — testeable sin mocks. Acumulado del mes en curso hasta hoy, contra el mismo tramo del
        // mes anterior (acotado a su propia cantidad de días si es más corto) y la proyección de
        // cierre al ritmo actual.
        public static DashboardThermometerDTO BuildThermometer(List<DaySpendingDTO> days, DateTime today)
        {
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var daysElapsed = today.Day;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            var amountByDate = days.ToDictionary(d => d.Date.Date, d => d.Amount);

            decimal SumRange(DateTime start, int dayCount)
            {
                decimal total = 0m;
                for (var i = 0; i < dayCount; i++)
                    if (amountByDate.TryGetValue(start.AddDays(i), out var amount)) total += amount;
                return total;
            }

            var monthToDate = SumRange(currentMonthStart, daysElapsed);

            var daysInPreviousMonth = DateTime.DaysInMonth(previousMonthStart.Year, previousMonthStart.Month);
            var previousMonthDaysToCompare = Math.Min(daysElapsed, daysInPreviousMonth);
            var previousMonthToDate = SumRange(previousMonthStart, previousMonthDaysToCompare);

            var projected = daysElapsed > 0 ? Math.Round(monthToDate / daysElapsed * daysInMonth, 2) : 0m;

            return new DashboardThermometerDTO
            {
                MonthToDateAmount = Math.Round(monthToDate, 2),
                PreviousMonthSameDayAmount = Math.Round(previousMonthToDate, 2),
                ProjectedMonthEndAmount = projected,
                DaysElapsed = daysElapsed,
                DaysInMonth = daysInMonth
            };
        }

        // Mismo criterio que el resto de los servicios de reporte (IncomeExpenseReportService,
        // CardReportService): el activo pedido tiene que ser una moneda.
        private async Task<Asset> GetCurrencyAssetAsync(int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            if (asset.AssetTypeId != 1)
                throw new BusinessRuleException("El activo no es una moneda");
            return asset;
        }
    }
}
