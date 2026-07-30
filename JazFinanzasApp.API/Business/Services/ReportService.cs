using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.Report;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class ReportService : IReportService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ISharedEventRepository _sharedEventRepository;

        public ReportService(
            ITransactionRepository transactionRepository,
            IAssetRepository assetRepository,
            IAsset_UserRepository asset_UserRepository,
            ICardTransactionRepository cardTransactionRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IAssetTypeRepository assetTypeRepository,
            IPortfolioRepository portfolioRepository,
            ITripRepository tripRepository,
            ISharedEventRepository sharedEventRepository)
        {
            _transactionRepository = transactionRepository;
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _assetTypeRepository = assetTypeRepository;
            _portfolioRepository = portfolioRepository;
            _tripRepository = tripRepository;
            _sharedEventRepository = sharedEventRepository;
        }

        public async Task<IEnumerable<TotalsBalanceDTO>> GetTotalsBalanceAsync(int userId)
        {
            var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(userId);
            var results = new List<TotalsBalanceResult>();

            if (!referenceAssets.Any())
            {
                var asset = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                results.Add(await _transactionRepository.GetTotalsBalanceByUserAsync(userId, asset));
            }
            else
            {
                foreach (var assetUser in referenceAssets)
                    results.Add(await _transactionRepository.GetTotalsBalanceByUserAsync(userId, assetUser.Asset));
            }

            return results.Select(r => new TotalsBalanceDTO
            {
                Asset = r.Asset,
                Symbol = r.Symbol,
                Color = r.Color,
                Balance = r.Balance
            });
        }

        public async Task<IEnumerable<BalanceDTO>> GetBalanceByAssetAsync(int userId, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            var results = await _transactionRepository.GetBalanceByAssetAndUserAsync(assetId, userId);
            return results.Select(r => new BalanceDTO { Account = r.Account, Balance = r.Balance });
        }

        public async Task<IncExpStatsDTO> GetIncExpStatsAsync(int userId, DateTime month, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");
            if (asset.AssetTypeId != 1)
                throw new BusinessRuleException("El activo no es una moneda");
            var result = await _transactionRepository.GetIncExpStatsAsync(userId, month, asset);
            return MapIncExpResult(result);
        }

        public async Task<CardsStatsDTO> GetCardStatsAsync(int userId, int cardId)
        {
            if (cardId != 0)
            {
                var card = await _assetRepository.GetByIdAsync(cardId)
                    ?? throw new NotFoundException("Card not found");
            }

            var today = DateTime.Now;
            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino");
            var exchangeRate = await _assetQuoteRepository.GetQuotePrice(peso.Id, today, "TARJETA");

            var pesosExpenses = await _cardTransactionRepository.GetCardStats(cardId, "Peso Argentino", userId);
            var dollarExpenses = await _cardTransactionRepository.GetCardStats(cardId, "Dolar Estadounidense", userId);
            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsToPay(cardId, today, userId);

            var cardPayments = cardTransactions.Select(m =>
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
                var valueInPesos = m.Asset.Name == "Dolar Estadounidense" ? m.InstallmentAmount * exchangeRate : m.InstallmentAmount;

                return new CardTransactionPaymentListDTO
                {
                    Date = m.Date,
                    Card = m.Card.Name,
                    TransactionClass = m.TransactionClass.Description,
                    Detail = m.Detail,
                    Asset = m.Asset.Name,
                    Installment = installmentDisplay,
                    InstallmentAmount = m.InstallmentAmount,
                    ValueInPesos = valueInPesos
                };
            }).ToList();

            return new CardsStatsDTO
            {
                PesosCardGraphDTO = pesosExpenses.Select(r => new CardGraphDTO { Month = r.Month, Amount = r.Amount }).ToArray(),
                DollarsCardGraphDTO = dollarExpenses.Select(r => new CardGraphDTO { Month = r.Month, Amount = r.Amount }).ToArray(),
                cardTransactionsDTO = cardPayments.ToArray()
            };
        }

        public async Task<StockStatsDTO> GetStockStatsAsync(int userId, int assetTypeId)
        {
            var assetType = await _assetRepository.GetByIdAsync(assetTypeId)
                ?? throw new NotFoundException("Asset type not found");

            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId;

            if (mainReferenceAsset == null)
            {
                var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollar.Id;
            }
            else
            {
                mainReferenceAssetId = mainReferenceAsset.AssetId;
            }

            var stockStats = await _transactionRepository.GetStockStatsAsync(userId, assetTypeId, "BOLSA", false, mainReferenceAssetId);
            var stockStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "BOLSA", mainReferenceAssetId);

            return new StockStatsDTO
            {
                StockStatsInd = stockStats.Select(r => new StockStatsListDTO
                {
                    AssetName = r.AssetName, Symbol = r.Symbol, Quantity = r.Quantity,
                    OriginalValue = r.OriginalValue, ActualValue = r.ActualValue
                }).ToArray(),
                StockStatsGral = stockStatsGral.Select(r => new StocksGralStatsDTO
                {
                    AssetType = r.AssetType, OriginalValue = r.OriginalValue, ActualValue = r.ActualValue
                }).ToArray()
            };
        }

        public async Task<CryptoGralStatsDTO> GetCryptoGralStatsAsync(int userId, bool includeStables)
        {
            var cryptoAsset = await _assetTypeRepository.GetByName("Criptomoneda");

            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId;

            if (mainReferenceAsset == null)
            {
                var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollar.Id;
            }
            else
            {
                mainReferenceAssetId = mainReferenceAsset.AssetId;
            }

            var cryptoGralStats = await _transactionRepository.GetStockStatsAsync(userId, cryptoAsset.Id, cryptoAsset.Environment, includeStables, mainReferenceAssetId);
            var cryptoStatsByDate = await _transactionRepository.GetCryptoStatsByDateAsync(userId, cryptoAsset.Id, cryptoAsset.Environment, 0, includeStables, mainReferenceAssetId);
            var cryptoPurchasesStatsByMonth = await _transactionRepository.GetInvestmentsHoldingsStats(userId, cryptoAsset.Id, cryptoAsset.Environment, 0, includeStables, 12, mainReferenceAssetId);

            return new CryptoGralStatsDTO
            {
                CryptoGralStats = cryptoGralStats.Select(r => new StockStatsListDTO
                {
                    AssetName = r.AssetName, Symbol = r.Symbol, Quantity = r.Quantity,
                    OriginalValue = r.OriginalValue, ActualValue = r.ActualValue
                }).ToArray(),
                CryptoStatsByDate = cryptoStatsByDate.Select(r => new CryptoStatsByDateDTO { Date = r.Date, Value = r.Value }).ToArray(),
                CryptoPurchasesStatsByMonth = cryptoPurchasesStatsByMonth.Select(r => new CryptoStatsByDateCommerceDTO
                {
                    Date = r.Date, CommerceType = r.CommerceType, Value = r.Value
                }).ToArray()
            };
        }

        public async Task<CryptoStatsDTO> GetCryptoStatsAsync(int userId, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId;

            if (mainReferenceAsset == null)
            {
                var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollar.Id;
            }
            else
            {
                mainReferenceAssetId = mainReferenceAsset.AssetId;
            }

            var cryptoEvolution = await _assetQuoteRepository.GetAssetEvolutionStats(assetId, 6, mainReferenceAssetId);
            var balance = await _transactionRepository.GetBalanceByAssetAndUserAsync(assetId, userId);
            var cryptoTransactionsStats = await _transactionRepository.GetInvestmentsTransactionsStats(userId, assetId, mainReferenceAssetId);
            var cryptoStatsEvolution = await _transactionRepository.GetCryptoStatsByDateAsync(userId, asset.AssetTypeId, "CRYPTO", assetId, true, mainReferenceAssetId);
            var averageBuyValue = await _transactionRepository.GetAverageBuyValue(userId, assetId, mainReferenceAssetId);

            var cryptoRangeStats = new InvestmentRangeValuesStatsDTO
            {
                MinValue = cryptoStatsEvolution.Where(m => m.Value > 0).Min(m => m.Value),
                MaxValue = cryptoStatsEvolution.Max(m => m.Value),
                CurrentValue = cryptoStatsEvolution.Last().Value,
                AverageBuyValue = averageBuyValue
            };

            return new CryptoStatsDTO
            {
                CryptoEvolutionStats = cryptoEvolution.Select(r => new CryptoStatsByDateDTO { Date = r.Date, Value = r.Value }).ToArray(),
                CryptoBalanceStats = balance.Select(r => new BalanceDTO { Account = r.Account, Balance = r.Balance }).ToArray(),
                CryptoTransactionsStats = cryptoTransactionsStats.Select(r => new InvestmentTransactionsStatsDTO
                {
                    Date = r.Date, Account = r.Account, MovementType = r.MovementType, CommerceType = r.CommerceType,
                    Quantity = r.Quantity, QuotePrice = r.QuotePrice, Total = r.Total
                }).ToArray(),
                CryptoRangeValuesStats = cryptoRangeStats
            };
        }

        public async Task<HomeStatsDTO> GetHomeStatsAsync(int userId)
        {
            var cryptoAsset = await _assetTypeRepository.GetByName("Criptomoneda");

            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            int mainReferenceAssetId;

            if (mainReferenceAsset == null)
            {
                var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
                mainReferenceAssetId = dollar.Id;
            }
            else
            {
                mainReferenceAssetId = mainReferenceAsset.AssetId;
            }

            var stockStatsGral = await _transactionRepository.GetStocksGralStatsAsync(userId, "BOLSA", mainReferenceAssetId);
            var cryptoStatsGral = await _transactionRepository.GetStockStatsAsync(userId, cryptoAsset.Id, "CRYPTO", true, mainReferenceAssetId);

            return new HomeStatsDTO
            {
                StockStatsGral = stockStatsGral.Select(r => new StocksGralStatsDTO
                {
                    AssetType = r.AssetType, OriginalValue = r.OriginalValue, ActualValue = r.ActualValue
                }).ToArray(),
                CryptoStatsGral = cryptoStatsGral.Select(r => new StockStatsListDTO
                {
                    AssetName = r.AssetName, Symbol = r.Symbol, Quantity = r.Quantity,
                    OriginalValue = r.OriginalValue, ActualValue = r.ActualValue
                }).ToArray()
            };
        }

        private async Task<int> GetMainReferenceAssetIdAsync(int userId)
        {
            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            if (mainReferenceAsset != null)
                return mainReferenceAsset.AssetId;

            var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
            return dollar.Id;
        }

        private async Task<Portfolio> GetOwnedPortfolioAsync(int userId, int portfolioId)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);
            if (portfolio == null || portfolio.UserId != userId)
                throw new NotFoundException("Portfolio not found");
            return portfolio;
        }

        public async Task<IEnumerable<PortfolioStatsDTO>> GetPortfolioStatsAsync(int userId)
        {
            var mainReferenceAssetId = await GetMainReferenceAssetIdAsync(userId);

            var portfolioStats = await _transactionRepository.GetPortfolioStatsAsync(userId, mainReferenceAssetId);

            return portfolioStats.Select(r => new PortfolioStatsDTO
            {
                PortfolioId = r.PortfolioId,
                PortfolioName = r.PortfolioName,
                IsDefault = r.IsDefault,
                OriginalValue = r.OriginalValue,
                ActualValue = r.ActualValue
            });
        }

        public async Task<PortfolioDetailStatsDTO> GetPortfolioDetailStatsAsync(int userId, int portfolioId)
        {
            var portfolio = await GetOwnedPortfolioAsync(userId, portfolioId);
            var mainReferenceAssetId = await GetMainReferenceAssetIdAsync(userId);

            // Reutiliza GetPortfolioStatsAsync (Fase 1) para el total de la cartera, en vez de recalcularlo
            // acá: garantiza por construcción que coincide con lo que muestra la columna de valor en
            // Tenencias, en vez de arriesgar una fórmula duplicada que diverja.
            var portfolioStats = await _transactionRepository.GetPortfolioStatsAsync(userId, mainReferenceAssetId);
            var portfolioStat = portfolioStats.FirstOrDefault(s => s.PortfolioId == portfolioId);

            var holdings = await _transactionRepository.GetPortfolioHoldingsAsync(userId, portfolioId, mainReferenceAssetId);

            return new PortfolioDetailStatsDTO
            {
                PortfolioId = portfolioId,
                PortfolioName = portfolio.Name,
                OriginalValue = portfolioStat?.OriginalValue ?? 0m,
                ActualValue = portfolioStat?.ActualValue ?? 0m,
                Holdings = holdings.Select(h => new PortfolioHoldingDTO
                {
                    AssetType = h.AssetType,
                    AssetName = h.AssetName,
                    Symbol = h.Symbol,
                    AccountName = h.AccountName,
                    Quantity = h.Quantity,
                    OriginalValue = h.OriginalValue,
                    ActualValue = h.ActualValue
                }).ToArray()
            };
        }

        // Evolución mensual del valor de una cartera, últimos 12 meses (docs/plans/activos/portfolios-estadisticas.md, Fase 5).
        public async Task<IEnumerable<PortfolioValueByDateDTO>> GetPortfolioValueHistoryAsync(int userId, int portfolioId)
        {
            await GetOwnedPortfolioAsync(userId, portfolioId);
            var mainReferenceAssetId = await GetMainReferenceAssetIdAsync(userId);

            var history = await _transactionRepository.GetPortfolioValueByDateAsync(userId, portfolioId, mainReferenceAssetId, months: 12);

            return history.Select(r => new PortfolioValueByDateDTO { Date = r.Date, Value = r.Value });
        }

        // Estadísticas básicas de Viajes (docs/plans/activos/plan-viajes.md, Fase 6; revisado en
        // docs/plans/completados/backfill-bariloche-2026.md para pasar de bruto a neto). "Total" es el neto
        // según Eventos Compartidos — lo que el usuario realmente consumió, sin importar quién pagó ni si ya
        // se saldó la deuda — no lo que salió de sus cuentas/tarjetas. Un viaje sin ningún Evento vinculado
        // da Total = 0 (no hay forma de derivar "lo que me costó" sin el reparto del evento). Conversión a la
        // moneda de referencia principal reusando el mismo puente por USD que ya usan las stats de Carteras.
        public async Task<IEnumerable<TripsGeneralStatsDTO>> GetTripsGeneralStatsAsync(int userId)
        {
            var mainReferenceAssetId = await GetMainReferenceAssetIdAsync(userId);
            var trips = await _tripRepository.GetByUserIdAsync(userId);

            var result = new List<TripsGeneralStatsDTO>();
            foreach (var trip in trips)
            {
                var nets = await GetTripMovementNetsAsync(trip.Id, mainReferenceAssetId);
                result.Add(new TripsGeneralStatsDTO
                {
                    TripId = trip.Id,
                    Name = trip.Name,
                    Type = trip.Type,
                    StartDate = trip.StartDate,
                    EndDate = trip.EndDate,
                    Status = GetTripStatus(trip),
                    TotalInReference = Math.Round(nets.Sum(n => n.ValueInReference), 2)
                });
            }

            return result.OrderByDescending(r => r.StartDate).ToList();
        }

        public async Task<TripDetailStatsDTO> GetTripDetailStatsAsync(int userId, int tripId)
        {
            var trip = await _tripRepository.GetByIdAsync(tripId)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();

            var mainReferenceAssetId = await GetMainReferenceAssetIdAsync(userId);
            var nets = await GetTripMovementNetsAsync(tripId, mainReferenceAssetId);

            var breakdown = nets
                .GroupBy(n => n.TransactionClass ?? "Sin clase")
                .Select(g => new TripClassBreakdownDTO { TransactionClass = g.Key, Amount = Math.Round(g.Sum(n => n.ValueInReference), 2) })
                .OrderByDescending(b => b.Amount)
                .ToArray();

            var eventBreakdown = nets
                .GroupBy(n => new { n.EventId, n.EventName })
                .Select(g => new TripEventNetDTO { EventId = g.Key.EventId, EventName = g.Key.EventName, Amount = Math.Round(g.Sum(n => n.ValueInReference), 2) })
                .ToArray();

            return new TripDetailStatsDTO
            {
                TripId = tripId,
                Name = trip.Name,
                Total = Math.Round(nets.Sum(n => n.ValueInReference), 2),
                Breakdown = breakdown,
                NetBreakdown = eventBreakdown
            };
        }

        private class TripMovementNet
        {
            public string? TransactionClass { get; set; }
            public int EventId { get; set; }
            public string EventName { get; set; } = string.Empty;
            public decimal ValueInReference { get; set; }
        }

        // Neto de Eventos Compartidos vinculados a un viaje (docs/plans/activos/plan-viajes-eventos.md, D1/D2),
        // movimiento por movimiento (no el agregado por evento) porque la cotización depende de la fecha de
        // cada movimiento. "Consumido" es la misma definición que SharedEventService.ComputeBalances usa para
        // la parte del usuario (Shares.Where(PersonId == null)), sin reinventar la fórmula. Se devuelve por
        // movimiento (no ya sumado) para poder agrupar tanto por clase (Breakdown) como por evento (NetBreakdown)
        // sin recorrer los movimientos ni pedir cotizaciones dos veces.
        private async Task<List<TripMovementNet>> GetTripMovementNetsAsync(int tripId, int referenceAssetId)
        {
            var events = await _sharedEventRepository.GetDetailByTripIdAsync(tripId);

            var result = new List<TripMovementNet>();
            foreach (var e in events)
            {
                foreach (var m in e.Movements ?? new List<SharedEventMovement>())
                {
                    var userAmount = m.Shares?.Where(s => s.PersonId == null).Sum(s => s.Amount) ?? 0;
                    if (userAmount == 0) continue;

                    var valueInUsd = m.Asset?.Name == "Dolar Estadounidense"
                        ? userAmount
                        : userAmount / await _assetQuoteRepository.GetQuotePrice(m.AssetId, m.Date, "BLUE");
                    var referenceQuote = await GetReferenceQuoteAsync(referenceAssetId, m.Date);

                    result.Add(new TripMovementNet
                    {
                        TransactionClass = m.TransactionClass?.Description,
                        EventId = e.Id,
                        EventName = e.Name,
                        ValueInReference = valueInUsd * referenceQuote
                    });
                }
            }

            return result;
        }

        // USD es la moneda puente: no tiene cotización contra sí misma, se resuelve como identidad.
        private async Task<decimal> GetReferenceQuoteAsync(int referenceAssetId, DateTime date)
        {
            var referenceAsset = await _assetRepository.GetByIdAsync(referenceAssetId);
            if (referenceAsset.Symbol == "USD") return 1m;

            var type = referenceAsset.Symbol == "ARS" ? "BLUE" : "NA";
            return await _assetQuoteRepository.GetQuotePrice(referenceAssetId, date, type);
        }

        private static string GetTripStatus(Trip trip)
        {
            var today = DateTime.UtcNow.Date;
            if (today < trip.StartDate.Date) return "PLANNED";
            if (today > trip.EndDate.Date) return "FINISHED";
            return "IN_PROGRESS";
        }

        private static IncExpStatsDTO MapIncExpResult(IncExpResult r) => new IncExpStatsDTO
        {
            ClassIncomeStats = r.ClassIncomeStats?.Select(x => new ClassIncomeStats { TransactionClass = x.TransactionClass, Amount = x.Amount }).ToArray(),
            ClassExpenseStats = r.ClassExpenseStats?.Select(x => new ClassExpenseStats { TransactionClass = x.TransactionClass, Amount = x.Amount }).ToArray(),
            MonthIncomeStats = r.MonthIncomeStats?.Select(x => new MonthIncomeStats { Month = x.Month, Amount = x.Amount }).ToArray(),
            MonthExpenseStats = r.MonthExpenseStats?.Select(x => new MonthExpenseStats { Month = x.Month, Amount = x.Amount }).ToArray()
        };
    }
}
