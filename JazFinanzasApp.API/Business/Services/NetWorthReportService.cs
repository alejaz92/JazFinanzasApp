using JazFinanzasApp.API.Business.DTO.NetWorth;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class NetWorthReportService : INetWorthReportService
    {
        private const int MonthlySeriesLength = 12;
        // T9: "supera los 3 días hábiles" (D-7) aproximado a 5 días de calendario, para no tener
        // que resolver feriados — un fin de semana de por medio ya cubre los 3 hábiles reales.
        private const int StaleDaysThreshold = 5;

        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ICardPaymentRepository _cardPaymentRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;

        public NetWorthReportService(
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            ICardPaymentRepository cardPaymentRepository,
            IAssetRepository assetRepository,
            IAssetQuoteRepository assetQuoteRepository,
            IAsset_UserRepository asset_UserRepository)
        {
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _cardPaymentRepository = cardPaymentRepository;
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
            _asset_UserRepository = asset_UserRepository;
        }

        // El bruto es GetTotalsBalanceByUserAsync sin tocar (T7) — mismo número que "Saldos" por
        // construcción, no por comparación después.
        public async Task<NetWorthGeneralDTO> GetGeneralAsync(int userId)
        {
            var referenceAssets = await GetReferenceAssetsOrDefaultAsync(userId);
            var debtInDollars = await GetLiveCardDebtInDollarsAsync(userId);
            var staleAssets = await _transactionRepository.GetStaleAssetsAsync(userId, StaleDaysThreshold);

            var totals = new List<NetWorthTotalDTO>();
            foreach (var asset in referenceAssets)
            {
                var gross = await _transactionRepository.GetTotalsBalanceByUserAsync(userId, asset);
                var (rate, _) = await _transactionRepository.GetReferenceAssetRateAsync(asset);
                var debtInAsset = asset.Name == "Dolar Estadounidense" ? debtInDollars : debtInDollars * rate;

                totals.Add(new NetWorthTotalDTO
                {
                    Asset = gross.Asset,
                    Symbol = gross.Symbol,
                    Color = gross.Color,
                    GrossBalance = gross.Balance,
                    CardDebt = Math.Round(debtInAsset, 2),
                    NetBalance = Math.Round(gross.Balance - debtInAsset, 2)
                });
            }

            return new NetWorthGeneralDTO
            {
                Totals = totals,
                StaleAssets = staleAssets.Select(s => new StaleAssetDTO { AssetName = s.AssetName, QuoteDate = s.QuoteDate }).ToList()
            };
        }

        public async Task<IEnumerable<NetWorthMonthlyPointDTO>> GetMonthlySeriesAsync(int userId, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var points = await _transactionRepository.GetNetWorthMonthlySeriesAsync(userId, asset, MonthlySeriesLength);
            return points.Select(p => new NetWorthMonthlyPointDTO
            {
                Month = p.Month,
                Accounts = p.Accounts,
                Stocks = p.Stocks,
                CryptoStable = p.CryptoStable,
                CryptoVolatile = p.CryptoVolatile,
                Bonds = p.Bonds
            });
        }

        public async Task<IEnumerable<AccountBalanceDTO>> GetByAccountAsync(int userId, int assetId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Asset not found");

            var accounts = await _transactionRepository.GetAccountBalancesAsync(userId, asset, MonthlySeriesLength);
            return accounts.Select(a => new AccountBalanceDTO
            {
                AccountId = a.AccountId,
                AccountName = a.AccountName,
                Balance = a.Balance,
                Evolution = a.Evolution.Select(e => new MonthlyBalanceDTO { Month = e.Month, Balance = e.Balance }).ToList(),
                Holdings = a.Holdings.Select(h => new AccountHoldingDTO
                {
                    AssetId = h.AssetId,
                    AssetName = h.AssetName,
                    AssetSymbol = h.AssetSymbol,
                    NativeBalance = h.NativeBalance,
                    BalanceInReferenceAsset = h.BalanceInReferenceAsset
                }).ToList()
            });
        }

        // T8 — deuda de tarjeta viva: suma de las cuotas todavía no vencidas de los consumos cuya
        // última cuota es futura, valuadas con la cotización del consumo (no la de hoy).
        public async Task<decimal> GetLiveCardDebtInDollarsAsync(int userId)
        {
            var cardTransactions = await _cardTransactionRepository.FindAsync(ct => ct.UserId == userId);
            var lastPaidByCard = await _cardPaymentRepository.GetLastPaidMonthByCardAsync(userId);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            decimal total = 0m;
            foreach (var ct in cardTransactions)
            {
                var liveMonths = CountLiveInstallmentMonths(ct, lastPaidByCard, currentMonth);
                if (liveMonths == 0) continue;

                var asset = await _assetRepository.GetByIdAsync(ct.AssetId);
                var amountInDollars = asset.Name == "Dolar Estadounidense"
                    ? ct.InstallmentAmount
                    : ct.InstallmentAmount / await _assetQuoteRepository.GetQuotePrice(ct.AssetId, ct.Date, "TARJETA");

                total += amountInDollars * liveMonths;
            }

            return Math.Round(total, 2);
        }

        // Pura y sin dependencias de infraestructura — testeable con datos en memoria.
        // "YES" (recurrente sin fin): solo se cuenta la cuota del mes en curso si todavía no se pagó
        // (no un compromiso infinito hacia adelante). "NO"/"CLOSED" (rango fijo): cada mes de
        // FirstInstallment a LastInstallment que sea posterior al último mes pagado de esa tarjeta.
        public static int CountLiveInstallmentMonths(CardTransaction cardTransaction, Dictionary<int, DateTime> lastPaidMonthByCard, DateTime currentMonth)
        {
            var hasPayment = lastPaidMonthByCard.TryGetValue(cardTransaction.CardId, out var lastPaid);

            if (cardTransaction.Repeat == "YES")
            {
                var firstInstallmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1);
                var nextDue = !hasPayment ? firstInstallmentMonth : lastPaid.AddMonths(1);
                return nextDue <= currentMonth ? 1 : 0;
            }

            var count = 0;
            for (var i = 0; i < cardTransaction.Installments; i++)
            {
                var installmentMonth = new DateTime(cardTransaction.FirstInstallment.Year, cardTransaction.FirstInstallment.Month, 1).AddMonths(i);
                if (!hasPayment || installmentMonth > lastPaid) count++;
            }
            return count;
        }

        private async Task<IEnumerable<Asset>> GetReferenceAssetsOrDefaultAsync(int userId)
        {
            var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(userId);
            if (referenceAssets.Any()) return referenceAssets.Select(a => a.Asset);

            var dollar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense");
            return new[] { dollar };
        }
    }
}
