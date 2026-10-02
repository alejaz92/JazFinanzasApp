using JazFinanzasApp.API.Business.DTO.BondCollection;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Cobros de bonos (plan-amortizaciones-bonos.md, Etapa 2). Cada pago del cronograma (BondPayment)
    // se registra, por tenencia (cuenta + cartera), como un BondCollection con hasta dos movimientos
    // de Transaction: capital (categoría de sistema "Ingreso Inversiones", no cuenta como ingreso) e
    // interés (categoría que elige el usuario, tiene que contar como ingreso).
    public class BondCollectionService : IBondCollectionService
    {
        private readonly IBondPaymentRepository _bondPaymentRepository;
        private readonly IBondCollectionRepository _bondCollectionRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IQuotePriceResolver _quotePriceResolver;
        private readonly IUnitOfWork _unitOfWork;

        public BondCollectionService(
            IBondPaymentRepository bondPaymentRepository,
            IBondCollectionRepository bondCollectionRepository,
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            IPortfolioRepository portfolioRepository,
            ITransactionClassRepository transactionClassRepository,
            IQuotePriceResolver quotePriceResolver,
            IUnitOfWork unitOfWork)
        {
            _bondPaymentRepository = bondPaymentRepository;
            _bondCollectionRepository = bondCollectionRepository;
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _portfolioRepository = portfolioRepository;
            _transactionClassRepository = transactionClassRepository;
            _quotePriceResolver = quotePriceResolver;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BondCollectionPendingDTO>> GetPendingAsync(int userId)
        {
            var pastPayments = (await _bondPaymentRepository.GetPastAsync(DateTime.Today)).ToList();
            if (pastPayments.Count == 0) return Enumerable.Empty<BondCollectionPendingDTO>();

            var holdingsByPayment = new Dictionary<int, List<BondHoldingResult>>();
            foreach (var payment in pastPayments)
            {
                var holdings = (await _transactionRepository.GetBondHoldingsBeforeDateAsync(userId, payment.AssetId, payment.PaymentDate)).ToList();
                if (holdings.Count > 0) holdingsByPayment[payment.Id] = holdings;
            }

            var resolvedKeys = (await _bondCollectionRepository.GetByUserIdAsync(userId))
                .Select(c => (c.BondPaymentId, c.AccountId, c.PortfolioId))
                .ToHashSet();

            return BuildPending(pastPayments, holdingsByPayment, resolvedKeys);
        }

        // Pura — testeable sin mocks (T9). `holdingsByPayment` ya trae, para cada BondPayment, las
        // combinaciones (cuenta, cartera) con tenencia viva antes de la fecha de pago; `resolvedKeys`
        // ya trae qué combinaciones tienen cualquier BondCollection (Registered/Untracked/Dismissed
        // cuentan igual como resueltas).
        public static List<BondCollectionPendingDTO> BuildPending(
            IEnumerable<BondPayment> pastPayments,
            Dictionary<int, List<BondHoldingResult>> holdingsByPayment,
            HashSet<(int BondPaymentId, int AccountId, int PortfolioId)> resolvedKeys)
        {
            var pending = new List<BondCollectionPendingDTO>();
            foreach (var payment in pastPayments)
            {
                if (!holdingsByPayment.TryGetValue(payment.Id, out var holdings)) continue;
                foreach (var holding in holdings)
                {
                    if (resolvedKeys.Contains((payment.Id, holding.AccountId, holding.PortfolioId))) continue;
                    pending.Add(BuildPendingItem(payment, holding));
                }
            }
            return pending;
        }

        // Pura — testeable sin mocks (T5, T6). Estimación sin redondear a lo que se cobra en bruto:
        // capital = tenencia x 100 x fracción amortizada; interés = tenencia x interés por 100. Si el
        // bono es indexado (CER/dólar linked), ninguno de los dos se estima: el monto no se conoce de
        // antemano y el formulario lo pide a mano.
        public static BondCollectionPendingDTO BuildPendingItem(BondPayment payment, BondHoldingResult holding)
        {
            decimal? capital = payment.IsIndexed ? null : Math.Round(holding.Quantity * 100 * payment.AmortizationRate, 2);
            decimal? interest = payment.IsIndexed ? null : Math.Round(holding.Quantity * payment.InterestPer100, 2);

            return new BondCollectionPendingDTO
            {
                BondPaymentId = payment.Id,
                AssetId = payment.AssetId,
                AssetName = payment.Asset.Name,
                Symbol = payment.Asset.Symbol,
                PaymentDate = payment.PaymentDate,
                AccountId = holding.AccountId,
                AccountName = holding.AccountName,
                PortfolioId = holding.PortfolioId,
                PortfolioName = holding.PortfolioName,
                HeldQuantity = holding.Quantity,
                CurrencySymbol = payment.CurrencyAsset.Symbol,
                IsIndexed = payment.IsIndexed,
                EstimatedCapital = capital,
                EstimatedInterest = interest
            };
        }

        public async Task<IEnumerable<BondCollectionListDTO>> GetRegisteredAsync(int userId)
        {
            var collections = await _bondCollectionRepository.GetByUserIdAsync(userId);
            return collections.Select(c => new BondCollectionListDTO
            {
                Id = c.Id,
                AssetId = c.BondPayment.AssetId,
                AssetName = c.BondPayment.Asset.Name,
                Symbol = c.BondPayment.Asset.Symbol,
                PaymentDate = c.BondPayment.PaymentDate,
                AccountName = c.Account.Name,
                PortfolioName = c.Portfolio.Name,
                Status = c.Status,
                CollectionDate = c.CollectionDate,
                CapitalAmount = c.CapitalAmount,
                InterestAmount = c.InterestAmount,
                CurrencySymbol = c.BondPayment.CurrencyAsset.Symbol
            });
        }

        public async Task<int> RegisterAsync(int userId, BondCollectionRegisterDTO dto)
        {
            if (dto.CapitalAmount < 0 || dto.InterestAmount < 0)
                throw new BusinessRuleException("Los montos no pueden ser negativos");
            if (dto.CapitalAmount == 0 && dto.InterestAmount == 0)
                throw new BusinessRuleException("Al menos uno de los montos tiene que ser mayor a cero");

            var payment = await _bondPaymentRepository.GetByIdAsync(dto.BondPaymentId)
                ?? throw new NotFoundException("Pago de bono no encontrado");
            if (payment.PaymentDate > DateTime.Today)
                throw new BusinessRuleException("Este pago todavía no ocurrió");

            var account = await _accountRepository.GetByIdAsync(dto.AccountId)
                ?? throw new NotFoundException("Cuenta no encontrada");
            if (account.UserId != userId) throw new UnauthorizedDomainException();

            var portfolio = await _portfolioRepository.GetByIdAsync(dto.PortfolioId)
                ?? throw new NotFoundException("Cartera no encontrada");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();

            var holdings = await _transactionRepository.GetBondHoldingsBeforeDateAsync(userId, payment.AssetId, payment.PaymentDate);
            var holding = holdings.FirstOrDefault(h => h.AccountId == dto.AccountId && h.PortfolioId == dto.PortfolioId)
                ?? throw new BusinessRuleException("No había tenencia de este bono en esa cuenta y cartera antes de la fecha de pago");

            var existing = await _bondCollectionRepository.GetByUserIdAsync(userId);
            if (existing.Any(c => c.BondPaymentId == dto.BondPaymentId && c.AccountId == dto.AccountId && c.PortfolioId == dto.PortfolioId))
                throw new BusinessRuleException("Ya existe un registro para este cobro");

            TransactionClass? interestClass = null;
            if (dto.InterestAmount > 0)
            {
                if (dto.InterestTransactionClassId == null)
                    throw new BusinessRuleException("Falta la categoría del interés");
                interestClass = await _transactionClassRepository.GetByIdAsync(dto.InterestTransactionClassId.Value)
                    ?? throw new NotFoundException("Transaction class not found");
                if (interestClass.UserId != userId) throw new UnauthorizedDomainException();
                if (interestClass.IncExp != "I")
                    throw new BusinessRuleException("La categoría del interés tiene que ser de tipo ingreso");
                if (!interestClass.CountsAsIncomeExpense)
                    throw new BusinessRuleException("La categoría del interés tiene que contar como ingreso en Reportes");
            }

            var capitalClass = dto.CapitalAmount > 0
                ? await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Ingreso Inversiones", userId)
                    ?? throw new BusinessRuleException("Transaction class 'Ingreso Inversiones' not found")
                : null;

            var quotePrice = await _quotePriceResolver.ResolveAsync(payment.CurrencyAssetId, dto.CollectionDate);
            var symbol = payment.Asset.Symbol;
            var dateLabel = payment.PaymentDate.ToString("dd/MM/yyyy");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var collection = await _bondCollectionRepository.AddAsyncReturnObject(new BondCollection
                {
                    BondPaymentId = payment.Id,
                    UserId = userId,
                    AccountId = dto.AccountId,
                    PortfolioId = dto.PortfolioId,
                    Status = BondCollectionStatus.Registered,
                    HeldQuantity = holding.Quantity,
                    CollectionDate = dto.CollectionDate,
                    CapitalAmount = dto.CapitalAmount,
                    InterestAmount = dto.InterestAmount,
                    QuotePrice = quotePrice
                });

                if (dto.CapitalAmount > 0)
                {
                    await _transactionRepository.AddAsync(new Transaction
                    {
                        AccountId = dto.AccountId,
                        PortfolioId = dto.PortfolioId,
                        AssetId = payment.CurrencyAssetId,
                        Date = dto.CollectionDate,
                        MovementType = "I",
                        TransactionClassId = capitalClass!.Id,
                        BondCollectionId = collection.Id,
                        Detail = $"Amortización {symbol} — pago {dateLabel}",
                        Amount = dto.CapitalAmount,
                        UserId = userId,
                        QuotePrice = quotePrice
                    });
                }

                if (dto.InterestAmount > 0)
                {
                    await _transactionRepository.AddAsync(new Transaction
                    {
                        AccountId = dto.AccountId,
                        PortfolioId = dto.PortfolioId,
                        AssetId = payment.CurrencyAssetId,
                        Date = dto.CollectionDate,
                        MovementType = "I",
                        TransactionClassId = interestClass!.Id,
                        BondCollectionId = collection.Id,
                        Detail = $"Interés {symbol} — pago {dateLabel}",
                        Amount = dto.InterestAmount,
                        UserId = userId,
                        QuotePrice = quotePrice
                    });
                }

                await _unitOfWork.CommitTransactionAsync();
                return collection.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DismissAsync(int userId, BondCollectionDismissDTO dto)
        {
            var payment = await _bondPaymentRepository.GetByIdAsync(dto.BondPaymentId)
                ?? throw new NotFoundException("Pago de bono no encontrado");
            if (payment.PaymentDate > DateTime.Today)
                throw new BusinessRuleException("Este pago todavía no ocurrió");

            var account = await _accountRepository.GetByIdAsync(dto.AccountId)
                ?? throw new NotFoundException("Cuenta no encontrada");
            if (account.UserId != userId) throw new UnauthorizedDomainException();

            var portfolio = await _portfolioRepository.GetByIdAsync(dto.PortfolioId)
                ?? throw new NotFoundException("Cartera no encontrada");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();

            var existing = await _bondCollectionRepository.GetByUserIdAsync(userId);
            if (existing.Any(c => c.BondPaymentId == dto.BondPaymentId && c.AccountId == dto.AccountId && c.PortfolioId == dto.PortfolioId))
                throw new BusinessRuleException("Ya existe un registro para este cobro");

            var holdings = await _transactionRepository.GetBondHoldingsBeforeDateAsync(userId, payment.AssetId, payment.PaymentDate);
            var holding = holdings.FirstOrDefault(h => h.AccountId == dto.AccountId && h.PortfolioId == dto.PortfolioId);

            await _bondCollectionRepository.AddAsyncReturnObject(new BondCollection
            {
                BondPaymentId = payment.Id,
                UserId = userId,
                AccountId = dto.AccountId,
                PortfolioId = dto.PortfolioId,
                Status = BondCollectionStatus.Dismissed,
                HeldQuantity = holding?.Quantity ?? 0,
                CollectionDate = null,
                CapitalAmount = 0,
                InterestAmount = 0,
                QuotePrice = null
            });
        }

        public async Task DeleteAsync(int userId, int id)
        {
            var collection = await _bondCollectionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Cobro no encontrado");
            if (collection.UserId != userId) throw new UnauthorizedDomainException();

            var transactions = await _transactionRepository.GetByBondCollectionIdAsync(id);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var transaction in transactions)
                    await _transactionRepository.DeleteAsync(transaction.Id);

                await _bondCollectionRepository.DeleteAsync(id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<int?> GetLastInterestClassIdAsync(int userId)
        {
            return await _transactionRepository.GetLastBondInterestTransactionClassIdAsync(userId);
        }
    }
}
