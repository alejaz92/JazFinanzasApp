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
        //
        // Corrección 2026-09-05: MonthlySeries se devuelve convertida a `assetId` — la serie sigue
        // separando "cuánto salió de pesos" de "cuánto salió de dólares" (BuildMonthlyConsumptionSeries
        // no cambia, sigue siendo pura y en moneda nativa), pero cada número ya viene expresado en la
        // moneda elegida, con la cotización HISTÓRICA de cada mes (no la de hoy — un año de inflación
        // de por medio arruinaría los meses viejos, T6/T9 del plan).
        public async Task<CardGeneralReportDTO> GetGeneralAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var dollar = await _assetRepository.GetAssetByNameAsync(DollarAssetName);

            var monthlySeries = BuildMonthlyConsumptionSeries(transactions, today, MonthlySeriesLength);
            await ConvertSeriesToReferenceCurrencyAsync(monthlySeries, peso, dollar, referenceAsset);

            return new CardGeneralReportDTO
            {
                ReferenceAssetSymbol = referenceAsset.Symbol,
                PesoAssetSymbol = peso.Symbol,
                PesoAssetColor = peso.Color,
                DollarAssetSymbol = dollar.Symbol,
                DollarAssetColor = dollar.Color,
                MonthlySeries = monthlySeries,
                CurrentMonthSummary = await BuildMonthSummaryAsync(userId, today)
            };
        }

        // Convierte in-place cada punto de la serie a `referenceAsset`, con la cotización del mes de
        // ESE punto (no la de hoy): dos lookups por mes (uno para pesos, otro para dólares), aplicados
        // a todas las tarjetas de ese mes — no uno por tarjeta, la tasa es la misma para todas. Si un
        // mes no tuvo gasto en una de las dos monedas, ni se pide la cotización (no hace falta, y un
        // mes sin consumo en esa moneda podría no tener cotización "TARJETA" cargada ese día).
        private async Task ConvertSeriesToReferenceCurrencyAsync(List<CardMonthlySeriesPointDTO> series, Asset peso, Asset dollar, Asset referenceAsset)
        {
            foreach (var point in series)
            {
                var hasPesos = point.Cards.Any(c => c.PesosAmount != 0);
                var hasDollars = point.Cards.Any(c => c.DollarsAmount != 0);

                var pesoRate = hasPesos ? await GetConversionRateAsync(peso, referenceAsset, point.Month) : 1m;
                var dollarRate = hasDollars ? await GetConversionRateAsync(dollar, referenceAsset, point.Month) : 1m;

                foreach (var card in point.Cards)
                {
                    card.PesosAmount = Math.Round(card.PesosAmount * pesoRate, 2);
                    card.DollarsAmount = Math.Round(card.DollarsAmount * dollarRate, 2);
                }
            }
        }

        // Misma idea que ConvertSeriesToReferenceCurrencyAsync pero para una serie "plana" (un solo
        // Pesos/Dólares por punto, sin apertura por tarjeta) — la usa Por tarjeta → Evolución mensual.
        private async Task ConvertSimpleSeriesToReferenceCurrencyAsync(List<CardSimpleMonthlyPointDTO> series, Asset peso, Asset dollar, Asset referenceAsset)
        {
            foreach (var point in series)
            {
                var pesoRate = point.PesosAmount != 0 ? await GetConversionRateAsync(peso, referenceAsset, point.Month) : 1m;
                var dollarRate = point.DollarsAmount != 0 ? await GetConversionRateAsync(dollar, referenceAsset, point.Month) : 1m;
                point.PesosAmount = Math.Round(point.PesosAmount * pesoRate, 2);
                point.DollarsAmount = Math.Round(point.DollarsAmount * dollarRate, 2);
            }
        }

        // Multiplicador para pasar un monto en `nativeAsset` a `referenceAsset`, en la fecha dada.
        // GetQuotePrice(assetId, date, "TARJETA") da "unidades de assetId por 1 USD" (mismo criterio
        // que GetLiveCardDebtInDollarsAsync, Fase 10) — se arma la cadena nativa→USD→referencia para
        // que funcione con cualquier par, no solo peso/dólar.
        private async Task<decimal> GetConversionRateAsync(Asset nativeAsset, Asset referenceAsset, DateTime date)
        {
            if (nativeAsset.Id == referenceAsset.Id) return 1m;

            var nativeInUsd = nativeAsset.Name == DollarAssetName
                ? 1m
                : 1m / await _assetQuoteRepository.GetQuotePrice(nativeAsset.Id, date, "TARJETA");

            var referenceFromUsd = referenceAsset.Name == DollarAssetName
                ? 1m
                : await _assetQuoteRepository.GetQuotePrice(referenceAsset.Id, date, "TARJETA");

            return nativeInUsd * referenceFromUsd;
        }

        // Corrección 2026-09-05: el usuario pidió poder navegar el "resumen del mes" a meses
        // distintos del actual — la lógica ya era genérica sobre `month` (GetCardTransactionsToPay
        // ya lo era), solo hacía falta exponerla suelta. cardId = 0 (default) trae todas las
        // tarjetas, igual que antes; Por tarjeta pasa la suya (corrección 2026-09-05, segunda ronda).
        public async Task<List<CardTransactionPaymentListDTO>> GetMonthSummaryAsync(int userId, DateTime month, int cardId = 0)
        {
            var normalizedMonth = new DateTime(month.Year, month.Month, 1);
            return await BuildMonthSummaryAsync(userId, normalizedMonth, cardId);
        }

        public async Task<CardDetailReportDTO> GetByCardAsync(int userId, int cardId, int assetId)
        {
            var card = await _cardRepository.GetByIdAsync(cardId)
                ?? throw new NotFoundException("Tarjeta no encontrada");
            if (card.UserId != userId)
                throw new UnauthorizedDomainException();

            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var dollar = await _assetRepository.GetAssetByNameAsync(DollarAssetName);

            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var startMonth = today.AddMonths(-(MonthlySeriesLength - 1));

            var thisCard = transactions.Where(t => t.CardId == cardId).ToList();
            var currentMonthPesosNative = thisCard.Where(t => t.Asset?.Name == PesoAssetName).Sum(t => GetAccrualAmount(t, today));
            var currentMonthDollarsNative = thisCard.Where(t => t.Asset?.Name == DollarAssetName).Sum(t => GetAccrualAmount(t, today));

            // Una sola cotización de "hoy", reusada para el consumo del mes y para ByCategory (los
            // dos son totales de una fecha/agregado, no una serie mensual) — mismo criterio que
            // NetWorthReportService al pasar un total ya sumado a otra moneda de referencia (T7).
            var todayPesoRate = await GetConversionRateAsync(peso, referenceAsset, today);
            var todayDollarRate = await GetConversionRateAsync(dollar, referenceAsset, today);

            var byCategory = BuildCategoryBreakdown(transactions, cardId, startMonth, today);
            foreach (var category in byCategory)
            {
                category.PesosAmount = Math.Round(category.PesosAmount * todayPesoRate, 2);
                category.DollarsAmount = Math.Round(category.DollarsAmount * todayDollarRate, 2);
            }

            var evolution = BuildCardEvolution(transactions, cardId, today, MonthlySeriesLength);
            await ConvertSimpleSeriesToReferenceCurrencyAsync(evolution, peso, dollar, referenceAsset);

            return new CardDetailReportDTO
            {
                CardId = card.Id,
                CardName = card.Name,
                NextClosingDate = card.NextClosingDate,
                NextDueDate = card.NextDueDate,
                ReferenceAssetSymbol = referenceAsset.Symbol,
                PesoAssetSymbol = peso.Symbol,
                PesoAssetColor = peso.Color,
                DollarAssetSymbol = dollar.Symbol,
                DollarAssetColor = dollar.Color,
                CurrentMonthPesos = Math.Round(currentMonthPesosNative * todayPesoRate, 2),
                CurrentMonthDollars = Math.Round(currentMonthDollarsNative * todayDollarRate, 2),
                ByCategory = byCategory,
                MonthlyEvolution = evolution
            };
        }

        // T8 extendido (NetWorthReportService.CountLiveInstallmentMonths, Fase 10): la misma regla de
        // "qué cuota sigue viva" pero proyectada hacia adelante mes a mes, no solo contada.
        //
        // Corrección 2026-09-05: los montos vienen convertidos a `assetId`. Los meses son futuros y no
        // tienen cotización propia — GetQuotePrice cae en la más reciente disponible (T9), que
        // termina siendo la de hoy, así que se pide una sola vez para toda la proyección.
        public async Task<CardFutureCommitmentDTO> GetFutureCommitmentAsync(int userId, int assetId, bool includeRecurring = true, int cardId = 0)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var dollar = await _assetRepository.GetAssetByNameAsync(DollarAssetName);

            var allTransactions = await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId);
            var transactions = (cardId == 0 ? allTransactions : allTransactions.Where(t => t.CardId == cardId)).ToList();
            var lastPaidByCard = await _cardPaymentRepository.GetLastPaidMonthByCardAsync(userId);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var result = BuildFutureCommitment(transactions, lastPaidByCard, currentMonth, FutureCommitmentMonths, includeRecurring);

            var hasPesos = result.Timeline.Any(t => t.AssetName == PesoAssetName);
            var hasDollars = result.Timeline.Any(t => t.AssetName == DollarAssetName);
            var pesoRate = hasPesos ? await GetConversionRateAsync(peso, referenceAsset, currentMonth) : 1m;
            var dollarRate = hasDollars ? await GetConversionRateAsync(dollar, referenceAsset, currentMonth) : 1m;
            decimal RateFor(string assetName) => assetName == DollarAssetName ? dollarRate : assetName == PesoAssetName ? pesoRate : 1m;

            foreach (var month in result.MonthlySeries)
                foreach (var purchase in month.Purchases)
                    purchase.Amount = Math.Round(purchase.Amount * RateFor(purchase.AssetName), 2);

            foreach (var entry in result.Timeline)
                entry.InstallmentAmount = Math.Round(entry.InstallmentAmount * RateFor(entry.AssetName), 2);

            result.ReferenceAssetSymbol = referenceAsset.Symbol;
            result.PesoAssetSymbol = peso.Symbol;
            result.PesoAssetColor = peso.Color;
            result.DollarAssetSymbol = dollar.Symbol;
            result.DollarAssetColor = dollar.Color;

            return result;
        }

        // Corrección 2026-09-05: los montos vienen convertidos a `assetId`, con un criterio de fecha
        // distinto según qué representa cada uno — TotalSaved es un agregado histórico (cotización de
        // hoy, igual que ByCategory en Por tarjeta), MonthlySeries es una serie temporal (cotización de
        // cada mes) y Pending son eventos fechados puntuales (cotización de su propio CreditDate).
        public async Task<CardPromotionsReportDTO> GetPromotionsAsync(int userId, int assetId)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var dollar = await _assetRepository.GetAssetByNameAsync(DollarAssetName);

            var discounts = (await _cardTransactionDiscountRepository.GetByUserIdWithCardTransactionAsync(userId)).ToList();
            var transactions = (await _cardTransactionRepository.GetByUserIdWithDetailsAsync(userId)).ToList();

            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var startMonth = today.AddMonths(-(MonthlySeriesLength - 1));
            var consumptionInWindow = transactions
                .Where(t => { var m = new DateTime(t.Date.Year, t.Date.Month, 1); return m >= startMonth && m <= today; })
                .ToList();

            var report = BuildPromotionsReport(discounts, consumptionInWindow, today, MonthlySeriesLength);

            var todayPesoRate = await GetConversionRateAsync(peso, referenceAsset, today);
            var todayDollarRate = await GetConversionRateAsync(dollar, referenceAsset, today);
            report.TotalSavedPesos = Math.Round(report.TotalSavedPesos * todayPesoRate, 2);
            report.TotalSavedDollars = Math.Round(report.TotalSavedDollars * todayDollarRate, 2);

            foreach (var month in report.MonthlySeries)
            {
                var pesoRate = month.PesosAmount != 0 ? await GetConversionRateAsync(peso, referenceAsset, month.Month) : 1m;
                var dollarRate = month.DollarsAmount != 0 ? await GetConversionRateAsync(dollar, referenceAsset, month.Month) : 1m;
                month.PesosAmount = Math.Round(month.PesosAmount * pesoRate, 2);
                month.DollarsAmount = Math.Round(month.DollarsAmount * dollarRate, 2);
            }

            foreach (var pending in report.Pending)
            {
                var nativeAsset = pending.AssetName == DollarAssetName ? dollar : peso;
                var rate = await GetConversionRateAsync(nativeAsset, referenceAsset, pending.CreditDate);
                pending.PendingToCredit = Math.Round(pending.PendingToCredit * rate, 2);
                pending.PendingToApply = Math.Round(pending.PendingToApply * rate, 2);
            }

            report.ReferenceAssetSymbol = referenceAsset.Symbol;
            report.PesoAssetSymbol = peso.Symbol;
            report.PesoAssetColor = peso.Color;
            report.DollarAssetSymbol = dollar.Symbol;
            report.DollarAssetColor = dollar.Color;

            return report;
        }

        // Reusa exactamente la lógica de ReportService.GetCardStatsAsync (pantalla vieja, cardId = 0
        // para "todas las tarjetas"): mismo criterio de instalmentDisplay y de conversión a pesos.
        // Genérica sobre `month` desde el vamos — GetGeneralAsync la usa con el mes en curso y
        // GetMonthSummaryAsync con cualquier otro. `cardId` = 0 trae todas las tarjetas
        // (GetCardTransactionsToPay ya interpreta 0 así); Por tarjeta pasa una puntual.
        private async Task<List<CardTransactionPaymentListDTO>> BuildMonthSummaryAsync(int userId, DateTime month, int cardId = 0)
        {
            var peso = await _assetRepository.GetAssetByNameAsync(PesoAssetName);
            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, month, "TARJETA");
            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(cardId, month, userId);

            return cardTransactions.Select(m =>
            {
                string installmentDisplay;
                if (m.Repeat == "YES")
                {
                    installmentDisplay = "Recurrente";
                }
                else
                {
                    var currentInstallment = ((month.Year - m.FirstInstallment.Year) * 12) + month.Month - m.FirstInstallment.Month + 1;
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
        // Corrección 2026-09-05, quinta ronda: "YES" (recurrente sin fin, ej. una prepaga) ya NO usa
        // la simplificación de T8 de contar solo el mes en curso. T8 mide una DEUDA de hoy y ahí tiene
        // sentido no proyectar un compromiso infinito; este reporte es al revés, una proyección hacia
        // adelante — un gasto recurrente real (Swiss Medical, Netflix) se va a seguir cobrando TODOS
        // los meses de la ventana, no solo el próximo, y mostrarlo solo un mes subestimaba el
        // compromiso real (bug encontrado por el usuario viendo el gráfico con datos reales).
        public static List<DateTime> GetLiveInstallmentMonths(CardTransaction cardTransaction, Dictionary<int, DateTime> lastPaidMonthByCard, DateTime currentMonth, int monthsForward)
        {
            var windowEnd = currentMonth.AddMonths(monthsForward);
            var hasPayment = lastPaidMonthByCard.TryGetValue(cardTransaction.CardId, out var lastPaid);

            if (cardTransaction.Repeat == "YES")
            {
                var firstInstallmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1);
                var nextDue = !hasPayment ? firstInstallmentMonth : lastPaid.AddMonths(1);
                var start = nextDue > currentMonth ? nextDue : currentMonth;

                var recurrentMonths = new List<DateTime>();
                for (var m = start; m < windowEnd; m = m.AddMonths(1))
                    recurrentMonths.Add(m);
                return recurrentMonths;
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

        // Pura — testeable sin mocks. `includeRecurring` en false saca los gastos "YES" (Fase 15,
        // quinta ronda) — pedido del usuario porque una vez que un recurrente se proyecta correcto en
        // TODOS los meses (arriba), pasa a dominar el gráfico y tapa las compras en cuotas puntuales.
        public static CardFutureCommitmentDTO BuildFutureCommitment(List<CardTransaction> transactions, Dictionary<int, DateTime> lastPaidByCard, DateTime currentMonth, int monthsForward, bool includeRecurring = true)
        {
            var monthBuckets = Enumerable.Range(0, monthsForward).Select(i => currentMonth.AddMonths(i)).ToList();
            var purchasesByMonth = monthBuckets.ToDictionary(m => m, _ => new List<FutureCommitmentPurchaseAmountDTO>());
            var timeline = new List<FutureCommitmentPurchaseDTO>();

            var relevantTransactions = includeRecurring ? transactions : transactions.Where(t => t.Repeat != "YES");

            foreach (var ct in relevantTransactions)
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
                        TransactionClassId = ct.TransactionClassId,
                        TransactionClassName = ct.TransactionClass?.Description ?? string.Empty,
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
                    AssetName = d.CardTransaction?.Asset?.Name ?? PesoAssetName,
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
