using JazFinanzasApp.API.Business.DTO.CardReport;
using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class CardReportService : ICardReportService
    {
        // Mismo largo que las series mensuales de Patrimonio e Ingresos y Egresos (Fases 10 y 12).
        private const int MonthlySeriesLength = 12;

        // El plan pide "12 a 18 meses" para el compromiso futuro (Flujo 4); se toma el techo para no
        // cortar la cuota más lejana ya conocida al escribir el plan (abril 2027, relevamiento 1.4).
        private const int FutureCommitmentMonths = 18;

        private const string PesoAssetName = "Peso Argentino";
        private const string DollarAssetName = "Dolar Estadounidense";

        private readonly ICardRepository _cardRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly ICardTransactionDiscountRepository _cardTransactionDiscountRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public CardReportService(
            ICardRepository cardRepository,
            ICardTransactionRepository cardTransactionRepository,
            ICardPaymentRepository cardPaymentRepository,
            ICardTransactionDiscountRepository cardTransactionDiscountRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository)
        {
            _cardRepository = cardRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _cardTransactionDiscountRepository = cardTransactionDiscountRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
        }

        // "General": consumo devengado (CLAUDE.md Backend, "Tarjetas: el consumo y sus cuotas" — se
        // mide por CardTransaction.Date/TotalAmount, no por cuándo se paga la cuota), apilado por mes
        // y por tarjeta, más la tabla del resumen del mes actual que ya traía la pantalla vieja.
        public async Task<CardGeneralReportDTO> GetGeneralAsync(int userId)
        {
            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            return new CardGeneralReportDTO
            {
                MonthlySeries = BuildMonthlyConsumptionSeries(transactions, today, MonthlySeriesLength),
                CurrentMonthSummary = await BuildCurrentMonthSummaryAsync(userId, today)
            };
        }

        public async Task<CardDetailReportDTO> GetByCardAsync(int userId, int cardId)
        {
            var card = await _cardRepository.GetByIdAsync(cardId)
                ?? throw new NotFoundException("Tarjeta no encontrada");
            if (card.UserId != userId)
                throw new UnauthorizedDomainException();

            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var startMonth = today.AddMonths(-(MonthlySeriesLength - 1));

            var thisCard = transactions.Where(t => t.CardId == cardId).ToList();

            return new CardDetailReportDTO
            {
                CardId = card.Id,
                CardName = card.Name,
                NextClosingDate = card.NextClosingDate,
                NextDueDate = card.NextDueDate,
                CurrentMonthPesos = Math.Round(thisCard.Where(t => t.Asset?.Name == PesoAssetName).Sum(t => GetAccrualAmount(t, today)), 2),
                CurrentMonthDollars = Math.Round(thisCard.Where(t => t.Asset?.Name == DollarAssetName).Sum(t => GetAccrualAmount(t, today)), 2),
                ByCategory = BuildCategoryBreakdown(transactions, cardId, startMonth, today),
                MonthlyEvolution = BuildCardEvolution(transactions, cardId, today, MonthlySeriesLength)
            };
        }

        // T8 extendido (NetWorthReportService.CountLiveInstallmentMonths, Fase 10): la misma regla de
        // "qué cuota sigue viva" pero proyectada hacia adelante mes a mes, no solo contada.
        public async Task<CardFutureCommitmentDTO> GetFutureCommitmentAsync(int userId)
        {
            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();
            var lastPaidByCard = await _cardPaymentRepository.GetLastPaidMonthByCardAsync(userId);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            return BuildFutureCommitment(transactions, lastPaidByCard, currentMonth, FutureCommitmentMonths);
        }

        public async Task<CardPromotionsReportDTO> GetPromotionsAsync(int userId)
        {
            var discounts = (await _cardTransactionDiscountRepository.GetByUserIdWithCardTransactionAsync(userId)).ToList();
            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();

            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var startMonth = today.AddMonths(-(MonthlySeriesLength - 1));
            var consumptionInWindow = transactions
                .Where(t => { var m = new DateTime(t.Date.Year, t.Date.Month, 1); return m >= startMonth && m <= today; })
                .ToList();

            return BuildPromotionsReport(discounts, consumptionInWindow, today, MonthlySeriesLength);
        }

        // Reusa exactamente la lógica de ReportService.GetCardStatsAsync (pantalla vieja, cardId = 0
        // para "todas las tarjetas"): mismo criterio de instalmentDisplay y de conversión a pesos.
        private async Task<List<CardTransactionPaymentListDTO>> BuildCurrentMonthSummaryAsync(int userId, DateTime today)
        {
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, today, "TARJETA");
            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(0, today, userId);

            return cardTransactions.Select(m =>
            {
                string installmentDisplay;
                if (m.Repeat == "YES")
                {
                    installmentDisplay = "Recurrente";
                }
                else
                {
                    var currentInstallment = ((today.Year - m.FirstInstallment.Year) * 12) + today.Month - m.FirstInstallment.Month + 1;
                    installmentDisplay = $"{currentInstallment}/{m.Installments}";
                }
                var valueInPesos = m.Asset.Name == DollarAssetName ? m.InstallmentAmount * exchangeRate : m.InstallmentAmount;

                return new CardTransactionPaymentListDTO
                {
                    CardTransactionId = m.Id,
                    Date = m.Date,
                    CardId = m.CardId,
                    Card = m.Card.Name,
                    TransactionClassId = m.TransactionClassId,
                    TransactionClass = m.TransactionClass.Description,
                    Detail = m.Detail,
                    AssetId = m.AssetId,
                    Asset = m.Asset.Name,
                    Installment = installmentDisplay,
                    InstallmentAmount = m.InstallmentAmount,
                    ValueInPesos = valueInPesos
                };
            })
            .OrderBy(x => x.Card)
            .ThenBy(x => x.Date)
            .ToList();
        }

        // Devengado de UN mes para una compra puntual (Fase 15, corregido tras la revisión visual del
        // reporte "General"): una compra de una vez o en cuotas fijas ("NO"/"CLOSED") se devenga una
        // sola vez, completa, en su fecha de compra (CLAUDE.md Backend: "fecha y monto reales del
        // gasto"). Una recurrente sin fin ("YES", ej. una suscripción) no tiene un TotalAmount que
        // devengar una vez — es un cargo que se repite todos los meses desde que arrancó, así que
        // devenga InstallmentAmount en CADA mes desde FirstInstallment en adelante, no solo en el mes
        // en que se cargó la fila. Sin este caso especial, una suscripción vieja desaparecía del
        // consumo devengado de todos los meses salvo el de su alta.
        private static decimal GetAccrualAmount(CardTransaction ct, DateTime month)
        {
            if (ct.Repeat == "YES")
            {
                var firstMonth = new DateTime(ct.FirstInstallment.Year, ct.FirstInstallment.Month, 1);
                return month >= firstMonth ? ct.InstallmentAmount : 0m;
            }

            var ctMonth = new DateTime(ct.Date.Year, ct.Date.Month, 1);
            return ctMonth == month ? ct.TotalAmount : 0m;
        }

        // Devengado total de una compra sobre una ventana [startMonth, endMonth] — usado donde hace
        // falta un total del período en vez de una serie mes a mes (composición por categoría).
        private static decimal GetAccrualTotalInWindow(CardTransaction ct, DateTime startMonth, DateTime endMonth)
        {
            if (ct.Repeat == "YES")
            {
                var firstMonth = new DateTime(ct.FirstInstallment.Year, ct.FirstInstallment.Month, 1);
                var effectiveStart = firstMonth > startMonth ? firstMonth : startMonth;
                if (effectiveStart > endMonth) return 0m;
                var months = (endMonth.Year - effectiveStart.Year) * 12 + endMonth.Month - effectiveStart.Month + 1;
                return ct.InstallmentAmount * months;
            }

            var ctMonth = new DateTime(ct.Date.Year, ct.Date.Month, 1);
            return ctMonth >= startMonth && ctMonth <= endMonth ? ct.TotalAmount : 0m;
        }

        private static bool HasAnyAccrualInWindow(CardTransaction ct, DateTime startMonth, DateTime endMonth)
        {
            if (ct.Repeat == "YES")
            {
                var firstMonth = new DateTime(ct.FirstInstallment.Year, ct.FirstInstallment.Month, 1);
                return firstMonth <= endMonth;
            }

            var ctMonth = new DateTime(ct.Date.Year, ct.Date.Month, 1);
            return ctMonth >= startMonth && ctMonth <= endMonth;
        }

        // Pura — testeable sin mocks. Pesos y dólares nunca se mezclan (sección 6, Flujo 4: "en pesos
        // y en dólares"), mismo criterio que ya usaba GetCardStatsAsync.
        public static List<CardMonthlySeriesPointDTO> BuildMonthlyConsumptionSeries(List<CardTransaction> transactions, DateTime latestMonth, int monthsBack)
        {
            var startMonth = latestMonth.AddMonths(-(monthsBack - 1));

            // Solo las tarjetas con algún consumo en la ventana (de una vez, o recurrente activo) —
            // una tarjeta sin actividad no agrega una serie de puros ceros.
            var cardsInWindow = transactions
                .Where(t => HasAnyAccrualInWindow(t, startMonth, latestMonth))
                .Select(t => (t.CardId, CardName: t.Card?.Name ?? string.Empty))
                .Distinct()
                .ToList();

            var points = new List<CardMonthlySeriesPointDTO>();
            for (var i = 0; i < monthsBack; i++)
            {
                var month = startMonth.AddMonths(i);

                var cards = cardsInWindow.Select(c => new CardMonthAmountDTO
                {
                    CardId = c.CardId,
                    CardName = c.CardName,
                    PesosAmount = Math.Round(transactions.Where(t => t.CardId == c.CardId && t.Asset?.Name == PesoAssetName).Sum(t => GetAccrualAmount(t, month)), 2),
                    DollarsAmount = Math.Round(transactions.Where(t => t.CardId == c.CardId && t.Asset?.Name == DollarAssetName).Sum(t => GetAccrualAmount(t, month)), 2)
                }).ToList();

                points.Add(new CardMonthlySeriesPointDTO { Month = month, Cards = cards });
            }

            return points;
        }

        // Pura — testeable sin mocks. Composición por categoría de una tarjeta sobre la ventana
        // [startMonth, latestMonth] (no serie mensual, total del período).
        public static List<CardCategoryAmountDTO> BuildCategoryBreakdown(List<CardTransaction> transactions, int cardId, DateTime startMonth, DateTime latestMonth)
        {
            return transactions
                .Where(t => t.CardId == cardId)
                .GroupBy(t => new { t.TransactionClassId, Name = t.TransactionClass?.Description ?? string.Empty })
                .Select(g => new CardCategoryAmountDTO
                {
                    TransactionClassId = g.Key.TransactionClassId,
                    TransactionClassName = g.Key.Name,
                    PesosAmount = Math.Round(g.Where(t => t.Asset?.Name == PesoAssetName).Sum(t => GetAccrualTotalInWindow(t, startMonth, latestMonth)), 2),
                    DollarsAmount = Math.Round(g.Where(t => t.Asset?.Name == DollarAssetName).Sum(t => GetAccrualTotalInWindow(t, startMonth, latestMonth)), 2)
                })
                .Where(c => c.PesosAmount != 0 || c.DollarsAmount != 0)
                .OrderByDescending(c => c.PesosAmount + c.DollarsAmount)
                .ToList();
        }

        // Pura — testeable sin mocks. Evolución mensual de una sola tarjeta (mismo criterio de
        // devengado que BuildMonthlyConsumptionSeries, sin la apertura por tarjeta).
        public static List<CardSimpleMonthlyPointDTO> BuildCardEvolution(List<CardTransaction> transactions, int cardId, DateTime latestMonth, int monthsBack)
        {
            var startMonth = latestMonth.AddMonths(-(monthsBack - 1));
            var thisCard = transactions.Where(t => t.CardId == cardId).ToList();
            var points = new List<CardSimpleMonthlyPointDTO>();

            for (var i = 0; i < monthsBack; i++)
            {
                var month = startMonth.AddMonths(i);

                points.Add(new CardSimpleMonthlyPointDTO
                {
                    Month = month,
                    PesosAmount = Math.Round(thisCard.Where(t => t.Asset?.Name == PesoAssetName).Sum(t => GetAccrualAmount(t, month)), 2),
                    DollarsAmount = Math.Round(thisCard.Where(t => t.Asset?.Name == DollarAssetName).Sum(t => GetAccrualAmount(t, month)), 2)
                });
            }

            return points;
        }

        // Pura y sin dependencias de infraestructura — testeable con datos en memoria, mismo patrón
        // que NetWorthReportService.CountLiveInstallmentMonths (T8). Devuelve los meses vivos (no
        // pagados) dentro de [currentMonth, currentMonth + monthsForward), en vez de solo contarlos:
        // acá hace falta saber CUÁLES meses, para poder apilarlos en la columna que corresponda.
        //
        // "YES" (recurrente sin fin) mantiene la misma simplificación de T8: no es un compromiso
        // infinito hacia adelante, solo entra el mes en curso si todavía no se pagó. A diferencia de
        // T8, acá no se cuentan meses ya vencidos anteriores al mes en curso — este reporte mira hacia
        // adelante, no la deuda viva total (para eso está Patrimonio → General).
        public static List<DateTime> GetLiveInstallmentMonths(CardTransaction cardTransaction, Dictionary<int, DateTime> lastPaidMonthByCard, DateTime currentMonth, int monthsForward)
        {
            var windowEnd = currentMonth.AddMonths(monthsForward);
            var hasPayment = lastPaidMonthByCard.TryGetValue(cardTransaction.CardId, out var lastPaid);

            if (cardTransaction.Repeat == "YES")
            {
                var firstInstallmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1);
                var nextDue = !hasPayment ? firstInstallmentMonth : lastPaid.AddMonths(1);
                return nextDue <= currentMonth && currentMonth < windowEnd
                    ? new List<DateTime> { currentMonth }
                    : new List<DateTime>();
            }

            var months = new List<DateTime>();
            for (var i = 0; i < cardTransaction.Installments; i++)
            {
                var installmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1).AddMonths(i);
                var isUnpaid = !hasPayment || installmentMonth > lastPaid;
                if (isUnpaid && installmentMonth >= currentMonth && installmentMonth < windowEnd)
                    months.Add(installmentMonth);
            }

            return months;
        }

        // Pura — testeable sin mocks.
        public static CardFutureCommitmentDTO BuildFutureCommitment(List<CardTransaction> transactions, Dictionary<int, DateTime> lastPaidByCard, DateTime currentMonth, int monthsForward)
        {
            var monthBuckets = Enumerable.Range(0, monthsForward).Select(i => currentMonth.AddMonths(i)).ToList();
            var purchasesByMonth = monthBuckets.ToDictionary(m => m, _ => new List<FutureCommitmentPurchaseAmountDTO>());
            var timeline = new List<FutureCommitmentPurchaseDTO>();

            foreach (var ct in transactions)
            {
                var liveMonths = GetLiveInstallmentMonths(ct, lastPaidByCard, currentMonth, monthsForward);
                if (liveMonths.Count == 0) continue;

                foreach (var month in liveMonths)
                {
                    purchasesByMonth[month].Add(new FutureCommitmentPurchaseAmountDTO
                    {
                        CardTransactionId = ct.Id,
                        Detail = ct.Detail ?? string.Empty,
                        CardName = ct.Card?.Name ?? string.Empty,
                        AssetName = ct.Asset?.Name ?? string.Empty,
                        Amount = ct.InstallmentAmount
                    });
                }

                timeline.Add(new FutureCommitmentPurchaseDTO
                {
                    CardTransactionId = ct.Id,
                    Detail = ct.Detail ?? string.Empty,
                    CardName = ct.Card?.Name ?? string.Empty,
                    AssetName = ct.Asset?.Name ?? string.Empty,
                    InstallmentAmount = ct.InstallmentAmount,
                    StartMonth = liveMonths.Min(),
                    EndMonth = liveMonths.Max(),
                    RemainingInstallments = liveMonths.Count
                });
            }

            return new CardFutureCommitmentDTO
            {
                MonthlySeries = monthBuckets.Select(m => new FutureCommitmentMonthDTO { Month = m, Purchases = purchasesByMonth[m] }).ToList(),
                Timeline = timeline.OrderBy(t => t.StartMonth).ToList()
            };
        }

        // Pura — testeable sin mocks. TotalSaved y Pending miran todo el historial (poco todavía,
        // 1.4); MonthlySeries y el consumo del porcentaje se acotan a la ventana de `monthsBack`.
        public static CardPromotionsReportDTO BuildPromotionsReport(List<CardTransactionDiscount> discounts, List<CardTransaction> consumptionInWindow, DateTime latestMonth, int monthsBack)
        {
            var startMonth = latestMonth.AddMonths(-(monthsBack - 1));

            decimal PesoAmount(CardTransactionDiscount d) => d.CardTransaction?.Asset?.Name == PesoAssetName ? d.Amount : 0m;
            decimal DollarAmount(CardTransactionDiscount d) => d.CardTransaction?.Asset?.Name == DollarAssetName ? d.Amount : 0m;

            var inWindow = discounts
                .Where(d => { var m = new DateTime(d.CreditDate.Year, d.CreditDate.Month, 1); return m >= startMonth && m <= latestMonth; })
                .ToList();

            var monthlySeries = new List<PromotionMonthDTO>();
            for (var i = 0; i < monthsBack; i++)
            {
                var month = startMonth.AddMonths(i);
                var monthDiscounts = inWindow.Where(d => d.CreditDate.Year == month.Year && d.CreditDate.Month == month.Month).ToList();
                monthlySeries.Add(new PromotionMonthDTO
                {
                    Month = month,
                    PesosAmount = Math.Round(monthDiscounts.Sum(PesoAmount), 2),
                    DollarsAmount = Math.Round(monthDiscounts.Sum(DollarAmount), 2)
                });
            }

            var totalSavedPesos = discounts.Sum(PesoAmount);
            var totalSavedDollars = discounts.Sum(DollarAmount);

            var consumptionPesos = consumptionInWindow.Where(t => t.Asset?.Name == PesoAssetName).Sum(t => t.TotalAmount);
            var consumptionDollars = consumptionInWindow.Where(t => t.Asset?.Name == DollarAssetName).Sum(t => t.TotalAmount);

            var pending = discounts
                .Where(d => d.AmountApplied < d.Amount)
                .Select(d => new PendingReimbursementDTO
                {
                    DiscountId = d.Id,
                    CardTransactionId = d.CardTransactionId,
                    Detail = d.CardTransaction?.Detail ?? string.Empty,
                    CardName = d.CardTransaction?.Card?.Name ?? string.Empty,
                    PendingToCredit = d.Amount - d.AmountMaterialized,
                    PendingToApply = d.AmountMaterialized - d.AmountApplied,
                    CreditDate = d.CreditDate
                })
                .OrderBy(p => p.CreditDate)
                .ToList();

            return new CardPromotionsReportDTO
            {
                TotalSavedPesos = Math.Round(totalSavedPesos, 2),
                TotalSavedDollars = Math.Round(totalSavedDollars, 2),
                PercentOfConsumptionPesos = consumptionPesos > 0 ? Math.Round(totalSavedPesos / consumptionPesos * 100, 2) : null,
                PercentOfConsumptionDollars = consumptionDollars > 0 ? Math.Round(totalSavedDollars / consumptionDollars * 100, 2) : null,
                MonthlySeries = monthlySeries,
                Pending = pending
            };
        }
    }
}
