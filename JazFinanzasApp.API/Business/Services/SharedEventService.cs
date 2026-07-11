using JazFinanzasApp.API.Business.DTO.CardTransaction;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class SharedEventService : ISharedEventService
    {
        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly ISharedEventMovementRepository _sharedEventMovementRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _assetUserRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ICardTransactionService _cardTransactionService;
        private readonly ISharedExpenseRepository _sharedExpenseRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISharedEventPaymentRepository _sharedEventPaymentRepository;

        public SharedEventService(
            ISharedEventRepository sharedEventRepository,
            ISharedEventMovementRepository sharedEventMovementRepository,
            IPersonRepository personRepository,
            IAssetRepository assetRepository,
            IAsset_UserRepository assetUserRepository,
            ITransactionClassRepository transactionClassRepository,
            IAccountRepository accountRepository,
            ICardRepository cardRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            ICardTransactionService cardTransactionService,
            ISharedExpenseRepository sharedExpenseRepository,
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork,
            ISharedEventPaymentRepository sharedEventPaymentRepository)
        {
            _sharedEventRepository = sharedEventRepository;
            _sharedEventMovementRepository = sharedEventMovementRepository;
            _personRepository = personRepository;
            _assetRepository = assetRepository;
            _assetUserRepository = assetUserRepository;
            _transactionClassRepository = transactionClassRepository;
            _accountRepository = accountRepository;
            _cardRepository = cardRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _cardTransactionService = cardTransactionService;
            _sharedExpenseRepository = sharedExpenseRepository;
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
            _sharedEventPaymentRepository = sharedEventPaymentRepository;
        }

        public async Task<IEnumerable<SharedEventListDTO>> GetAllForUserAsync(int userId, bool includeClosed)
        {
            var events = await _sharedEventRepository.GetByUserIdAsync(userId, includeClosed);
            return events.Select(MapToListDTO);
        }

        public async Task<SharedEventDTO> GetByIdAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedDetailAsync(userId, id);
            return MapToDTO(sharedEvent);
        }

        public async Task<SharedEventDTO> CreateAsync(int userId, SharedEventAddDTO dto)
        {
            var personIds = dto.PersonIds.Distinct().ToList();
            await ValidatePersonsAsync(userId, personIds);

            var sharedEvent = new SharedEvent
            {
                Name = dto.Name,
                Notes = dto.Notes,
                UserId = userId,
                Participants = personIds.Select(pid => new SharedEventParticipant { PersonId = pid }).ToList()
            };

            var created = await _sharedEventRepository.AddAsyncReturnObject(sharedEvent);
            var full = await _sharedEventRepository.GetDetailByIdAsync(created.Id);
            return MapToDTO(full!);
        }

        public async Task UpdateAsync(int userId, int id, SharedEventEditDTO dto)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, id);

            sharedEvent.Name = dto.Name;
            sharedEvent.Notes = dto.Notes;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task<SharedEventDTO> AddParticipantAsync(int userId, int id, SharedEventParticipantAddDTO dto)
        {
            await GetOwnedEventAsync(userId, id);
            await ValidatePersonsAsync(userId, new List<int> { dto.PersonId });

            var existing = await _sharedEventRepository.GetParticipantAsync(id, dto.PersonId);
            if (existing != null)
                throw new BusinessRuleException("La persona ya es participante del evento");

            await _sharedEventRepository.AddParticipantAsync(new SharedEventParticipant
            {
                SharedEventId = id,
                PersonId = dto.PersonId
            });

            var full = await _sharedEventRepository.GetDetailByIdAsync(id);
            return MapToDTO(full!);
        }

        public async Task RemoveParticipantAsync(int userId, int id, int personId)
        {
            await GetOwnedEventAsync(userId, id);

            var participant = await _sharedEventRepository.GetParticipantAsync(id, personId)
                ?? throw new NotFoundException("La persona no es participante del evento");

            if (await _sharedEventRepository.ParticipantHasActivityAsync(id, personId))
                throw new BusinessRuleException("No se puede quitar un participante con movimientos o pagos en el evento");

            await _sharedEventRepository.RemoveParticipantAsync(participant);
        }

        public async Task CloseAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedDetailAsync(userId, id);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento ya está cerrado");

            var movements = sharedEvent.Movements?.ToList() ?? new List<SharedEventMovement>();
            var balances = ComputeBalances(sharedEvent, movements);
            if (balances.Any(b => b.NetBalance != 0))
                throw new BusinessRuleException("No se puede cerrar el evento: hay saldos pendientes en el evento. Registrar los pagos faltantes primero.");

            var assetIds = movements.Select(m => m.AssetId).Distinct().ToList();
            foreach (var assetId in assetIds)
            {
                var creditMovements = await _sharedEventPaymentRepository.GetMovementsWithPendingCreditsAsync(id, assetId);
                var hasCredits = creditMovements.Any(m => m.SharedExpense!.Splits.Any(s => s.Amount - s.AmountReimbursed > 0));

                var debtMovements = await _sharedEventPaymentRepository.GetMovementsWithPendingDebtsAsync(id, assetId);
                var hasDebts = debtMovements.Any(m => m.Shares.Any(s => s.PersonId == null && s.Amount - s.AmountSettled > 0));

                if (hasCredits && hasDebts)
                    throw new BusinessRuleException("Hay ítems cruzados pendientes (a favor y en contra) en el evento; registrar una compensación interna antes de cerrar");
                if (hasCredits || hasDebts)
                    throw new BusinessRuleException("Hay ítems pendientes en el evento; registrar el pago correspondiente antes de cerrar");
            }

            sharedEvent.IsClosed = true;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task ReopenAsync(int userId, int id)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, id);

            sharedEvent.IsClosed = false;
            sharedEvent.UpdatedAt = DateTime.UtcNow;
            await _sharedEventRepository.UpdateAsync(sharedEvent);
        }

        public async Task DeleteAsync(int userId, int id)
        {
            await GetOwnedEventAsync(userId, id);

            if (await _sharedEventRepository.HasMovementsOrPaymentsAsync(id))
                throw new BusinessRuleException("No se puede eliminar un evento con movimientos o pagos");

            await _sharedEventRepository.DeleteEventWithParticipantsAsync(id);
        }

        public async Task<SharedEventMovementDTO> CreateMovementAsync(int userId, int sharedEventId, SharedEventMovementAddDTO dto)
        {
            var sharedEvent = await GetOwnedDetailAsync(userId, sharedEventId);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento está cerrado");

            ValidateMovementInput(sharedEvent, dto);

            var asset = await _assetRepository.GetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Moneda no encontrada");
            _ = await _assetUserRepository.GetUserAssetAsync(userId, dto.AssetId)
                ?? throw new UnauthorizedDomainException();
            var transactionClass = await _transactionClassRepository.GetByIdAsync(dto.TransactionClassId)
                ?? throw new NotFoundException("Categoría no encontrada");
            if (transactionClass.UserId != userId) throw new UnauthorizedDomainException();
            if (transactionClass.IncExp == "I")
                throw new BusinessRuleException("La categoría debe ser de tipo egreso");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var derived = await CreateDerivedRecordsAsync(userId, sharedEvent, dto, asset, transactionClass);

                var movement = new SharedEventMovement
                {
                    SharedEventId = sharedEventId,
                    Date = dto.Date,
                    Description = dto.Description,
                    TransactionClassId = dto.TransactionClassId,
                    AssetId = dto.AssetId,
                    TotalAmount = dto.TotalAmount,
                    PayerPersonId = dto.PayerPersonId,
                    TransactionId = derived.TransactionId,
                    CardTransactionId = derived.CardTransactionId,
                    SharedExpenseId = derived.SharedExpenseId,
                    Notes = dto.Notes,
                    UserId = userId,
                    Shares = BuildShares(dto, derived.SplitIdByPerson)
                };

                var created = await _sharedEventMovementRepository.AddAsyncReturnObject(movement);
                await _unitOfWork.CommitTransactionAsync();

                var full = await _sharedEventMovementRepository.GetDetailByIdAsync(created.Id);
                return MapMovementToDTO(full!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<SharedEventMovementDTO> UpdateMovementAsync(int userId, int sharedEventId, int movementId, SharedEventMovementAddDTO dto)
        {
            var sharedEvent = await GetOwnedDetailAsync(userId, sharedEventId);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento está cerrado");

            var movement = await _sharedEventMovementRepository.GetDetailByIdAsync(movementId)
                ?? throw new NotFoundException("Movimiento no encontrado");
            if (movement.SharedEventId != sharedEventId)
                throw new NotFoundException("Movimiento no encontrado");

            if (await _sharedEventMovementRepository.HasActivityAsync(movementId))
                throw new BusinessRuleException("No se puede editar un movimiento con pagos aplicados; elimine primero los pagos involucrados");

            ValidateMovementInput(sharedEvent, dto);

            var asset = await _assetRepository.GetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Moneda no encontrada");
            _ = await _assetUserRepository.GetUserAssetAsync(userId, dto.AssetId)
                ?? throw new UnauthorizedDomainException();
            var transactionClass = await _transactionClassRepository.GetByIdAsync(dto.TransactionClassId)
                ?? throw new NotFoundException("Categoría no encontrada");
            if (transactionClass.UserId != userId) throw new UnauthorizedDomainException();
            if (transactionClass.IncExp == "I")
                throw new BusinessRuleException("La categoría debe ser de tipo egreso");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await DeleteDerivedRecordsAsync(movement);

                var derived = await CreateDerivedRecordsAsync(userId, sharedEvent, dto, asset, transactionClass);

                movement.Date = dto.Date;
                movement.Description = dto.Description;
                movement.TransactionClassId = dto.TransactionClassId;
                movement.AssetId = dto.AssetId;
                movement.TotalAmount = dto.TotalAmount;
                movement.PayerPersonId = dto.PayerPersonId;
                movement.TransactionId = derived.TransactionId;
                movement.CardTransactionId = derived.CardTransactionId;
                movement.SharedExpenseId = derived.SharedExpenseId;
                movement.Notes = dto.Notes;
                movement.UpdatedAt = DateTime.UtcNow;
                movement.Shares = BuildShares(dto, derived.SplitIdByPerson);

                await _sharedEventMovementRepository.UpdateAsync(movement);
                await _unitOfWork.CommitTransactionAsync();

                var full = await _sharedEventMovementRepository.GetDetailByIdAsync(movement.Id);
                return MapMovementToDTO(full!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DeleteMovementAsync(int userId, int sharedEventId, int movementId)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, sharedEventId);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento está cerrado");

            var movement = await _sharedEventMovementRepository.GetDetailByIdAsync(movementId)
                ?? throw new NotFoundException("Movimiento no encontrado");
            if (movement.SharedEventId != sharedEventId)
                throw new NotFoundException("Movimiento no encontrado");

            if (await _sharedEventMovementRepository.HasActivityAsync(movementId))
                throw new BusinessRuleException("No se puede eliminar un movimiento con pagos aplicados; elimine primero los pagos involucrados");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await DeleteDerivedRecordsAsync(movement);
                await _sharedEventMovementRepository.DeleteAsync(movement.Id);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<SharedEventActiveSummaryDTO>> GetActiveSummaryAsync(int userId)
        {
            var events = await _sharedEventRepository.GetOpenEventsDetailAsync(userId);

            return events.Select(e =>
            {
                var movements = e.Movements?.ToList() ?? new List<SharedEventMovement>();
                var balances = ComputeBalances(e, movements);

                return new SharedEventActiveSummaryDTO
                {
                    EventId = e.Id,
                    Name = e.Name,
                    Balances = balances
                        .Where(b => b.PersonId == null)
                        .Select(b => new SharedEventActiveSummaryBalanceDTO
                        {
                            AssetId = b.AssetId,
                            AssetName = b.AssetName,
                            AssetSymbol = b.AssetSymbol,
                            MyBalance = b.NetBalance
                        }).ToList()
                };
            }).ToList();
        }

        public async Task<IEnumerable<SharedEventConsolidatedDebtDTO>> GetConsolidatedDebtsAsync(int userId)
        {
            var events = await _sharedEventRepository.GetOpenEventsDetailAsync(userId);

            var eventNet = new Dictionary<(int PersonId, int AssetId), decimal>();
            var assetInfo = new Dictionary<int, (string Name, string Symbol)>();
            var personInfo = new Dictionary<int, string>();

            foreach (var e in events)
            {
                var movements = e.Movements?.ToList() ?? new List<SharedEventMovement>();
                var balances = ComputeBalances(e, movements);

                foreach (var b in balances.Where(b => b.PersonId != null))
                {
                    var key = (b.PersonId!.Value, b.AssetId);
                    eventNet[key] = eventNet.GetValueOrDefault(key) + b.NetBalance;
                    assetInfo[b.AssetId] = (b.AssetName, b.AssetSymbol);
                    personInfo[b.PersonId.Value] = b.PersonName ?? string.Empty;
                }
            }

            var pendingSplits = await _sharedExpenseRepository.GetPendingSplitsByUserIdAsync(userId);
            var loosePending = new Dictionary<(int PersonId, int AssetId), decimal>();

            foreach (var split in pendingSplits)
            {
                var assetId = split.SharedExpense.Transaction?.AssetId ?? split.SharedExpense.CardTransaction?.AssetId;
                if (assetId == null) continue;

                var key = (split.PersonId, assetId.Value);
                loosePending[key] = loosePending.GetValueOrDefault(key) + (split.Amount - split.AmountReimbursed);

                if (!personInfo.ContainsKey(split.PersonId))
                    personInfo[split.PersonId] = split.Person?.Alias ?? split.Person?.Name ?? string.Empty;

                if (!assetInfo.ContainsKey(assetId.Value))
                {
                    var asset = await _assetRepository.GetByIdAsync(assetId.Value);
                    assetInfo[assetId.Value] = (asset?.Name ?? string.Empty, asset?.Symbol ?? string.Empty);
                }
            }

            var allKeys = eventNet.Keys.Concat(loosePending.Keys).Distinct().ToList();
            var result = new List<SharedEventConsolidatedDebtDTO>();

            foreach (var key in allKeys)
            {
                var loose = loosePending.GetValueOrDefault(key);
                var eventBalance = eventNet.GetValueOrDefault(key);
                var combined = Math.Round(loose - eventBalance, 2);
                if (combined == 0) continue;

                result.Add(new SharedEventConsolidatedDebtDTO
                {
                    PersonId = key.PersonId,
                    PersonName = personInfo.GetValueOrDefault(key.PersonId, string.Empty),
                    AssetId = key.AssetId,
                    AssetName = assetInfo[key.AssetId].Name,
                    AssetSymbol = assetInfo[key.AssetId].Symbol,
                    PendingInFavor = combined > 0 ? combined : 0,
                    PendingAgainst = combined < 0 ? -combined : 0
                });
            }

            return result.OrderBy(r => r.PersonName).ThenBy(r => r.AssetId).ToList();
        }

        // ── Derivación contable ──────────────────────────────────────────────

        private sealed record DerivedRecords(int? TransactionId, int? CardTransactionId, int? SharedExpenseId, Dictionary<int, int> SplitIdByPerson);

        private async Task<DerivedRecords> CreateDerivedRecordsAsync(int userId, SharedEvent sharedEvent, SharedEventMovementAddDTO dto, Asset asset, TransactionClass transactionClass)
        {
            if (dto.PayerPersonId != null)
                return new DerivedRecords(null, null, null, new Dictionary<int, int>());

            var thirdPartyShares = dto.Shares.Where(s => s.PersonId != null).ToList();
            var detail = $"(Evento: {sharedEvent.Name}) {dto.Description}";

            if (dto.Payment!.AccountId.HasValue)
            {
                var account = await _accountRepository.GetByIdAsync(dto.Payment.AccountId.Value)
                    ?? throw new NotFoundException("Cuenta no encontrada");
                if (account.UserId != userId) throw new UnauthorizedDomainException();

                var portfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                    ?? throw new NotFoundException("Portfolio por defecto no encontrado");

                var balance = await _transactionRepository.GetBalance(account.Id, asset.Id, portfolio.Id);
                if (balance < dto.TotalAmount)
                    throw new BusinessRuleException("No hay suficiente saldo en la cuenta");

                var transaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
                {
                    AccountId = account.Id,
                    PortfolioId = portfolio.Id,
                    AssetId = asset.Id,
                    Date = dto.Date,
                    MovementType = "E",
                    TransactionClassId = transactionClass.Id,
                    Detail = detail,
                    Amount = -dto.TotalAmount,
                    UserId = userId
                });

                var (sharedExpenseId, splitIdByPerson) = await CreateSharedExpenseForAccountAsync(userId, transaction.Id, thirdPartyShares);
                return new DerivedRecords(transaction.Id, null, sharedExpenseId, splitIdByPerson);
            }
            else
            {
                var card = await _cardRepository.GetByIdAsync(dto.Payment.CardId!.Value)
                    ?? throw new NotFoundException("Tarjeta no encontrada");
                if (card.UserId != userId) throw new UnauthorizedDomainException();

                var installments = dto.Payment.Installments!.Value;
                var firstInstallment = new DateTime(dto.Payment.FirstInstallment!.Value.Year, dto.Payment.FirstInstallment.Value.Month, 1);
                var lastInstallment = firstInstallment.AddMonths(installments - 1);

                var cardTransactionId = await _cardTransactionService.AddCardTransactionAsync(userId, new CardTransactionAddDTO
                {
                    Date = dto.Date,
                    Detail = detail,
                    CardId = card.Id,
                    TransactionClassId = transactionClass.Id,
                    AssetId = asset.Id,
                    TotalAmount = dto.TotalAmount,
                    Installments = installments,
                    FirstInstallment = firstInstallment,
                    LastInstallment = lastInstallment,
                    Repeat = "NO"
                });

                var (sharedExpenseId, splitIdByPerson) = await CreateSharedExpenseForCardAsync(userId, cardTransactionId, installments, thirdPartyShares);
                return new DerivedRecords(null, cardTransactionId, sharedExpenseId, splitIdByPerson);
            }
        }

        private async Task<(int? SharedExpenseId, Dictionary<int, int> SplitIdByPerson)> CreateSharedExpenseForAccountAsync(
            int userId, int transactionId, List<SharedEventMovementShareInputDTO> thirdPartyShares)
        {
            if (!thirdPartyShares.Any())
                return (null, new Dictionary<int, int>());

            var sharedExpense = new SharedExpense
            {
                TransactionId = transactionId,
                UserId = userId,
                Splits = thirdPartyShares.Select(s => new SharedExpenseSplit
                {
                    PersonId = s.PersonId!.Value,
                    Amount = s.Amount,
                    AmountReimbursed = 0,
                    Status = SharedExpenseSplitStatus.Pending
                }).ToList()
            };

            var created = await _sharedExpenseRepository.AddAsyncReturnObject(sharedExpense);
            return (created.Id, created.Splits.ToDictionary(s => s.PersonId, s => s.Id));
        }

        private async Task<(int? SharedExpenseId, Dictionary<int, int> SplitIdByPerson)> CreateSharedExpenseForCardAsync(
            int userId, int cardTransactionId, int installments, List<SharedEventMovementShareInputDTO> thirdPartyShares)
        {
            if (!thirdPartyShares.Any())
                return (null, new Dictionary<int, int>());

            var sharedExpense = new SharedExpense
            {
                CardTransactionId = cardTransactionId,
                UserId = userId,
                Splits = thirdPartyShares.Select(s => new SharedExpenseSplit
                {
                    PersonId = s.PersonId!.Value,
                    Amount = s.Amount,
                    AmountReimbursed = 0,
                    AmountApplied = 0,
                    InstallmentSplitAmount = Math.Round(s.Amount / installments, 2),
                    Status = SharedExpenseSplitStatus.Pending
                }).ToList()
            };

            var created = await _sharedExpenseRepository.AddAsyncReturnObject(sharedExpense);
            return (created.Id, created.Splits.ToDictionary(s => s.PersonId, s => s.Id));
        }

        private static List<SharedEventMovementShare> BuildShares(SharedEventMovementAddDTO dto, Dictionary<int, int> splitIdByPerson)
        {
            return dto.Shares.Select(s => new SharedEventMovementShare
            {
                PersonId = s.PersonId,
                Amount = s.Amount,
                AmountSettled = 0,
                SharedExpenseSplitId = s.PersonId != null && splitIdByPerson.TryGetValue(s.PersonId.Value, out var splitId)
                    ? splitId
                    : (int?)null
            }).ToList();
        }

        private async Task DeleteDerivedRecordsAsync(SharedEventMovement movement)
        {
            if (movement.Shares.Any())
                await _sharedEventMovementRepository.RemoveSharesAsync(movement.Shares.ToList());
            movement.Shares.Clear();

            if (movement.SharedExpenseId != null)
                await _sharedExpenseRepository.DeleteByIdWithSplitsAsync(movement.SharedExpenseId.Value);

            if (movement.TransactionId != null)
                await _transactionRepository.DeleteAsync(movement.TransactionId.Value);

            if (movement.CardTransactionId != null)
                await _cardTransactionRepository.DeleteAsync(movement.CardTransactionId.Value);
        }

        // ── Validaciones ──────────────────────────────────────────────────────

        private static void ValidateMovementInput(SharedEvent sharedEvent, SharedEventMovementAddDTO dto)
        {
            if (dto.TotalAmount <= 0)
                throw new BusinessRuleException("El monto total debe ser mayor a cero");

            if (dto.Shares == null || !dto.Shares.Any())
                throw new BusinessRuleException("Debe incluir al menos una división del gasto");

            var participantIds = sharedEvent.Participants.Select(p => p.PersonId).ToHashSet();

            var seenUser = false;
            var seenPersons = new HashSet<int>();
            foreach (var share in dto.Shares)
            {
                if (share.Amount <= 0)
                    throw new BusinessRuleException("El monto de cada división debe ser mayor a cero");

                if (share.PersonId == null)
                {
                    if (seenUser) throw new BusinessRuleException("La parte del usuario está duplicada");
                    seenUser = true;
                }
                else
                {
                    if (!participantIds.Contains(share.PersonId.Value))
                        throw new BusinessRuleException($"La persona {share.PersonId} no es participante del evento");
                    if (!seenPersons.Add(share.PersonId.Value))
                        throw new BusinessRuleException("Hay una persona duplicada en las divisiones");
                }
            }

            var sharesTotal = dto.Shares.Sum(s => s.Amount);
            if (sharesTotal != dto.TotalAmount)
                throw new BusinessRuleException("La suma de las divisiones debe ser igual al monto total");

            if (dto.PayerPersonId != null && !participantIds.Contains(dto.PayerPersonId.Value))
                throw new BusinessRuleException("El pagador no es participante del evento");

            var paidByUser = dto.PayerPersonId == null;
            if (paidByUser && dto.Payment == null)
                throw new BusinessRuleException("Debe indicar cómo se pagó el movimiento");
            if (!paidByUser && dto.Payment != null)
                throw new BusinessRuleException("Un movimiento pagado por un tercero no debe indicar un medio de pago");

            if (paidByUser)
            {
                var isAccountMode = dto.Payment!.AccountId.HasValue;
                var isCardMode = dto.Payment.CardId.HasValue;
                if (isAccountMode == isCardMode)
                    throw new BusinessRuleException("Debe indicar exactamente una cuenta o una tarjeta para el pago");
                if (isCardMode && (dto.Payment.Installments is null || dto.Payment.Installments <= 0 || dto.Payment.FirstInstallment is null))
                    throw new BusinessRuleException("Debe indicar cuotas y primer vencimiento para el pago con tarjeta");
            }
        }

        private async Task ValidatePersonsAsync(int userId, List<int> personIds)
        {
            foreach (var personId in personIds)
            {
                var person = await _personRepository.GetByIdAsync(personId)
                    ?? throw new NotFoundException($"Persona {personId} no encontrada");
                if (person.UserId != userId)
                    throw new UnauthorizedDomainException();
            }
        }

        private async Task<SharedEvent> GetOwnedEventAsync(int userId, int id)
        {
            var sharedEvent = await _sharedEventRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            return sharedEvent;
        }

        private async Task<SharedEvent> GetOwnedDetailAsync(int userId, int id)
        {
            var sharedEvent = await _sharedEventRepository.GetDetailByIdAsync(id)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            return sharedEvent;
        }

        // ── Mapeo ─────────────────────────────────────────────────────────────

        private static SharedEventListDTO MapToListDTO(SharedEvent e)
        {
            return new SharedEventListDTO
            {
                Id = e.Id,
                Name = e.Name,
                IsClosed = e.IsClosed,
                ParticipantCount = e.Participants?.Count ?? 0,
                MovementCount = e.Movements?.Count ?? 0
            };
        }

        private static SharedEventDTO MapToDTO(SharedEvent e)
        {
            var movements = e.Movements?.OrderBy(m => m.Date).ToList() ?? new List<SharedEventMovement>();

            return new SharedEventDTO
            {
                Id = e.Id,
                Name = e.Name,
                Notes = e.Notes,
                IsClosed = e.IsClosed,
                Participants = e.Participants?
                    .OrderBy(p => p.Person?.Alias ?? p.Person?.Name)
                    .Select(p => new SharedEventParticipantDTO
                    {
                        PersonId = p.PersonId,
                        PersonName = p.Person?.Alias ?? p.Person?.Name ?? string.Empty
                    }).ToList() ?? new(),
                Movements = movements.Select(MapMovementToDTO).ToList(),
                Balances = ComputeBalances(e, movements),
                CategoryTotals = ComputeCategoryTotals(movements),
                Payments = (e.Payments ?? new List<SharedEventPayment>())
                    .OrderBy(p => p.Date).ThenBy(p => p.Id)
                    .Select(MapPaymentToDTO)
                    .ToList()
            };
        }

        private static SharedEventPaymentDTO MapPaymentToDTO(SharedEventPayment p)
        {
            return new SharedEventPaymentDTO
            {
                Id = p.Id,
                Date = p.Date,
                AssetId = p.AssetId,
                AssetName = p.Asset?.Name ?? string.Empty,
                AssetSymbol = p.Asset?.Symbol ?? string.Empty,
                Amount = p.Amount,
                FromPersonId = p.FromPersonId,
                FromPersonName = p.FromPerson?.Alias ?? p.FromPerson?.Name,
                ToPersonId = p.ToPersonId,
                ToPersonName = p.ToPerson?.Alias ?? p.ToPerson?.Name,
                AccountId = p.AccountId,
                IsInternalCompensation = p.IsInternalCompensation,
                Notes = p.Notes,
                Allocations = (p.Allocations ?? new List<SharedEventPaymentAllocation>()).Select(a => new SharedEventPaymentAllocationDTO
                {
                    Id = a.Id,
                    SplitId = a.SharedExpenseSplitId,
                    ShareId = a.SharedEventMovementShareId,
                    Amount = a.Amount
                }).ToList()
            };
        }

        private static SharedEventMovementDTO MapMovementToDTO(SharedEventMovement m)
        {
            return new SharedEventMovementDTO
            {
                Id = m.Id,
                Date = m.Date,
                Description = m.Description,
                TransactionClassId = m.TransactionClassId,
                TransactionClassName = m.TransactionClass?.Description ?? string.Empty,
                AssetId = m.AssetId,
                AssetName = m.Asset?.Name ?? string.Empty,
                AssetSymbol = m.Asset?.Symbol ?? string.Empty,
                TotalAmount = m.TotalAmount,
                PayerPersonId = m.PayerPersonId,
                PayerPersonName = m.PayerPerson?.Alias ?? m.PayerPerson?.Name,
                TransactionId = m.TransactionId,
                CardTransactionId = m.CardTransactionId,
                SharedExpenseId = m.SharedExpenseId,
                Notes = m.Notes,
                Shares = (m.Shares ?? new List<SharedEventMovementShare>())
                    .Select(s => new SharedEventMovementShareDTO
                    {
                        Id = s.Id,
                        PersonId = s.PersonId,
                        PersonName = s.Person?.Alias ?? s.Person?.Name,
                        Amount = s.Amount,
                        AmountSettled = s.AmountSettled,
                        Pending = Math.Round(s.Amount - s.AmountSettled, 2)
                    })
                    .OrderBy(s => s.PersonName ?? string.Empty)
                    .ToList()
            };
        }

        private static List<SharedEventBalanceDTO> ComputeBalances(SharedEvent e, List<SharedEventMovement> movements)
        {
            var payments = e.Payments?.ToList() ?? new List<SharedEventPayment>();

            var participantIds = e.Participants?.Select(p => (int?)p.PersonId).ToList() ?? new List<int?>();
            var personNames = e.Participants?.ToDictionary(p => (int?)p.PersonId, p => p.Person?.Alias ?? p.Person?.Name ?? string.Empty)
                ?? new Dictionary<int?, string>();

            var assetInfo = new Dictionary<int, (string Name, string Symbol)>();
            foreach (var m in movements)
                assetInfo[m.AssetId] = (m.Asset?.Name ?? string.Empty, m.Asset?.Symbol ?? string.Empty);
            foreach (var p in payments)
                if (!assetInfo.ContainsKey(p.AssetId))
                    assetInfo[p.AssetId] = (p.Asset?.Name ?? string.Empty, p.Asset?.Symbol ?? string.Empty);

            var assetIds = movements.Select(m => m.AssetId).Concat(payments.Select(p => p.AssetId)).Distinct().ToList();
            var people = new List<int?> { null }.Concat(participantIds).ToList();

            var result = new List<SharedEventBalanceDTO>();
            foreach (var assetId in assetIds)
            {
                foreach (var personId in people)
                {
                    var contributed = movements.Where(m => m.AssetId == assetId && m.PayerPersonId == personId).Sum(m => m.TotalAmount);
                    var consumed = movements.Where(m => m.AssetId == assetId)
                        .SelectMany(m => m.Shares ?? new List<SharedEventMovementShare>())
                        .Where(s => s.PersonId == personId)
                        .Sum(s => s.Amount);
                    var paymentsFrom = payments.Where(p => p.AssetId == assetId && p.FromPersonId == personId).Sum(p => p.Amount);
                    var paymentsTo = payments.Where(p => p.AssetId == assetId && p.ToPersonId == personId).Sum(p => p.Amount);

                    if (contributed == 0 && consumed == 0 && paymentsFrom == 0 && paymentsTo == 0)
                        continue;

                    result.Add(new SharedEventBalanceDTO
                    {
                        AssetId = assetId,
                        AssetName = assetInfo[assetId].Name,
                        AssetSymbol = assetInfo[assetId].Symbol,
                        PersonId = personId,
                        PersonName = personId == null ? null : personNames.GetValueOrDefault(personId),
                        Contributed = contributed,
                        Consumed = consumed,
                        NetBalance = Math.Round(contributed - consumed + paymentsFrom - paymentsTo, 2)
                    });
                }
            }

            return result.OrderBy(b => b.AssetId).ThenBy(b => b.PersonName ?? string.Empty).ToList();
        }

        private static List<SharedEventCategoryTotalDTO> ComputeCategoryTotals(List<SharedEventMovement> movements)
        {
            return movements
                .GroupBy(m => (m.AssetId, m.TransactionClassId))
                .Select(g =>
                {
                    var first = g.First();
                    return new SharedEventCategoryTotalDTO
                    {
                        AssetId = g.Key.AssetId,
                        AssetName = first.Asset?.Name ?? string.Empty,
                        AssetSymbol = first.Asset?.Symbol ?? string.Empty,
                        TransactionClassId = g.Key.TransactionClassId,
                        TransactionClassName = first.TransactionClass?.Description ?? string.Empty,
                        Total = g.Sum(m => m.TotalAmount)
                    };
                })
                .OrderBy(c => c.AssetId).ThenBy(c => c.TransactionClassName)
                .ToList();
        }
    }
}
