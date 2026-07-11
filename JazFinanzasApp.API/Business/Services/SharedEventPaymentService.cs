using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class SharedEventPaymentService : ISharedEventPaymentService
    {
        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly ISharedEventMovementRepository _sharedEventMovementRepository;
        private readonly ISharedEventPaymentRepository _sharedEventPaymentRepository;
        private readonly ISharedExpenseRepository _sharedExpenseRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SharedEventPaymentService(
            ISharedEventRepository sharedEventRepository,
            ISharedEventMovementRepository sharedEventMovementRepository,
            ISharedEventPaymentRepository sharedEventPaymentRepository,
            ISharedExpenseRepository sharedExpenseRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetRepository assetRepository,
            IAccountRepository accountRepository,
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork)
        {
            _sharedEventRepository = sharedEventRepository;
            _sharedEventMovementRepository = sharedEventMovementRepository;
            _sharedEventPaymentRepository = sharedEventPaymentRepository;
            _sharedExpenseRepository = sharedExpenseRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SharedEventPaymentPreviewDTO> PreviewPaymentAsync(int userId, int sharedEventId, SharedEventPaymentAddDTO dto)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, sharedEventId);
            ValidatePaymentInput(sharedEvent, dto);
            _ = await _assetRepository.GetByIdAsync(dto.AssetId) ?? throw new NotFoundException("Moneda no encontrada");

            if (IsThirdPartyOnly(dto))
                return new SharedEventPaymentPreviewDTO();

            var credits = await GetPendingCreditsAsync(sharedEventId, dto.AssetId);
            var debts = await GetPendingDebtsAsync(sharedEventId, dto.AssetId);

            var (creditAllocs, debtAllocs) = ResolveAllocations(dto, credits, debts);

            var items = new List<SharedEventPaymentPreviewItemDTO>();
            foreach (var (movement, split, amount) in creditAllocs)
            {
                var pendingBefore = split.Amount - split.AmountReimbursed;
                items.Add(new SharedEventPaymentPreviewItemDTO
                {
                    Kind = "Credit",
                    SplitId = split.Id,
                    MovementId = movement.Id,
                    MovementDescription = movement.Description,
                    MovementDate = movement.Date,
                    PersonId = split.PersonId,
                    PersonName = split.Person?.Alias ?? split.Person?.Name,
                    Amount = amount,
                    PendingBefore = pendingBefore,
                    PendingAfter = pendingBefore - amount
                });
            }
            foreach (var (movement, share, amount) in debtAllocs)
            {
                var pendingBefore = share.Amount - share.AmountSettled;
                items.Add(new SharedEventPaymentPreviewItemDTO
                {
                    Kind = "Debt",
                    ShareId = share.Id,
                    MovementId = movement.Id,
                    MovementDescription = movement.Description,
                    MovementDate = movement.Date,
                    PersonId = null,
                    PersonName = null,
                    Amount = amount,
                    PendingBefore = pendingBefore,
                    PendingAfter = pendingBefore - amount
                });
            }

            return new SharedEventPaymentPreviewDTO
            {
                CreditsAllocated = creditAllocs.Sum(c => c.Amount),
                DebtsAllocated = debtAllocs.Sum(d => d.Amount),
                Items = items.OrderBy(i => i.MovementDate).ToList()
            };
        }

        public async Task<SharedEventPaymentDTO> CreatePaymentAsync(int userId, int sharedEventId, SharedEventPaymentAddDTO dto)
        {
            var sharedEvent = await GetOwnedEventAsync(userId, sharedEventId);
            if (sharedEvent.IsClosed)
                throw new BusinessRuleException("El evento está cerrado");

            ValidatePaymentInput(sharedEvent, dto);
            _ = await _assetRepository.GetByIdAsync(dto.AssetId) ?? throw new NotFoundException("Moneda no encontrada");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                SharedEventPayment payment;

                if (IsThirdPartyOnly(dto))
                {
                    payment = new SharedEventPayment
                    {
                        SharedEventId = sharedEventId,
                        Date = dto.Date,
                        AssetId = dto.AssetId,
                        Amount = dto.Amount,
                        FromPersonId = dto.FromPersonId,
                        ToPersonId = dto.ToPersonId,
                        AccountId = dto.AccountId,
                        IsInternalCompensation = false,
                        Notes = dto.Notes,
                        UserId = userId,
                        Allocations = new List<SharedEventPaymentAllocation>()
                    };
                }
                else
                {
                    var account = await _accountRepository.GetByIdAsync(dto.AccountId!.Value)
                        ?? throw new NotFoundException("Cuenta no encontrada");
                    if (account.UserId != userId) throw new UnauthorizedDomainException();

                    var portfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                        ?? throw new NotFoundException("Portfolio por defecto no encontrado");

                    var credits = await GetPendingCreditsAsync(sharedEventId, dto.AssetId);
                    var debts = await GetPendingDebtsAsync(sharedEventId, dto.AssetId);
                    var (creditAllocs, debtAllocs) = ResolveAllocations(dto, credits, debts);

                    var allocations = new List<SharedEventPaymentAllocation>();
                    foreach (var (movement, split, amount) in creditAllocs)
                        allocations.AddRange(await ApplyCreditAsync(movement, split, amount, dto, portfolio, userId));

                    foreach (var (movement, share, amount) in debtAllocs)
                        allocations.Add(await ApplyDebtAsync(movement, share, amount, dto, sharedEvent.Name, portfolio, userId));

                    payment = new SharedEventPayment
                    {
                        SharedEventId = sharedEventId,
                        Date = dto.Date,
                        AssetId = dto.AssetId,
                        Amount = dto.Amount,
                        FromPersonId = dto.FromPersonId,
                        ToPersonId = dto.ToPersonId,
                        AccountId = dto.AccountId,
                        IsInternalCompensation = dto.IsInternalCompensation,
                        Notes = dto.Notes,
                        UserId = userId,
                        Allocations = allocations
                    };
                }

                var created = await _sharedEventPaymentRepository.AddAsyncReturnObject(payment);
                await _unitOfWork.CommitTransactionAsync();

                var full = await _sharedEventPaymentRepository.GetDetailByIdAsync(created.Id);
                return MapPaymentToDTO(full!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DeletePaymentAsync(int userId, int sharedEventId, int paymentId)
        {
            await GetOwnedEventAsync(userId, sharedEventId);

            var payment = await _sharedEventPaymentRepository.GetDetailByIdAsync(paymentId)
                ?? throw new NotFoundException("Pago no encontrado");
            if (payment.SharedEventId != sharedEventId)
                throw new NotFoundException("Pago no encontrado");

            var lastPayment = await _sharedEventPaymentRepository.GetLastPaymentAsync(sharedEventId);
            if (lastPayment == null || lastPayment.Id != paymentId)
                throw new BusinessRuleException("Solo se puede eliminar el último pago registrado del evento; para pagos anteriores, registrar un ajuste");

            foreach (var allocation in payment.Allocations.Where(a => a.CreatedIncomeTransactionId != null))
            {
                var stillExists = await _transactionRepository.GetByIdAsync(allocation.CreatedIncomeTransactionId!.Value);
                if (stillExists == null)
                    throw new BusinessRuleException("Este pago ya no se puede revertir: el reintegro fue consumido por un resumen de tarjeta posterior. Registrar un ajuste en su lugar.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var allocation in payment.Allocations)
                    await ReverseAllocationAsync(allocation);

                await _sharedEventPaymentRepository.DeletePaymentWithAllocationsAsync(paymentId);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        // ── D2: obtención de ítems pendientes (FIFO) ─────────────────────────

        private async Task<List<(SharedEventMovement Movement, SharedExpenseSplit Split)>> GetPendingCreditsAsync(int sharedEventId, int assetId)
        {
            var movements = await _sharedEventPaymentRepository.GetMovementsWithPendingCreditsAsync(sharedEventId, assetId);
            var items = new List<(SharedEventMovement, SharedExpenseSplit)>();
            foreach (var m in movements)
                foreach (var split in m.SharedExpense!.Splits.Where(s => s.Amount - s.AmountReimbursed > 0))
                    items.Add((m, split));
            return items.OrderBy(i => i.Item1.Date).ThenBy(i => i.Item2.Id).ToList();
        }

        private async Task<List<(SharedEventMovement Movement, SharedEventMovementShare Share)>> GetPendingDebtsAsync(int sharedEventId, int assetId)
        {
            var movements = await _sharedEventPaymentRepository.GetMovementsWithPendingDebtsAsync(sharedEventId, assetId);
            var items = new List<(SharedEventMovement, SharedEventMovementShare)>();
            foreach (var m in movements)
            {
                var userShare = m.Shares.FirstOrDefault(s => s.PersonId == null && s.Amount - s.AmountSettled > 0);
                if (userShare != null) items.Add((m, userShare));
            }
            return items.OrderBy(i => i.Item1.Date).ThenBy(i => i.Item2.Id).ToList();
        }

        private (List<(SharedEventMovement Movement, SharedExpenseSplit Split, decimal Amount)> Credits,
                 List<(SharedEventMovement Movement, SharedEventMovementShare Share, decimal Amount)> Debts)
            ResolveAllocations(
                SharedEventPaymentAddDTO dto,
                List<(SharedEventMovement Movement, SharedExpenseSplit Split)> credits,
                List<(SharedEventMovement Movement, SharedEventMovementShare Share)> debts)
        {
            var isIncoming = dto.ToPersonId == null && !dto.IsInternalCompensation;
            var isOutgoing = dto.FromPersonId == null && !dto.IsInternalCompensation;

            if (dto.Allocations != null && dto.Allocations.Any())
                return ResolveManualAllocations(dto, credits, debts, isIncoming);

            var totalC = credits.Sum(c => c.Split.Amount - c.Split.AmountReimbursed);
            var totalD = debts.Sum(d => d.Share.Amount - d.Share.AmountSettled);
            var (creditsAlloc, debtsAlloc) = ComputeDefaultAllocationAmounts(isIncoming, isOutgoing, dto.IsInternalCompensation, dto.Amount, totalC, totalD);

            return (DistributeCredits(credits, creditsAlloc), DistributeDebts(debts, debtsAlloc));
        }

        private static (decimal CreditsAlloc, decimal DebtsAlloc) ComputeDefaultAllocationAmounts(
            bool isIncoming, bool isOutgoing, bool isInternalCompensation, decimal amount, decimal totalC, decimal totalD)
        {
            if (isInternalCompensation)
            {
                var x = Math.Min(totalC, totalD);
                return (x, x);
            }
            if (isIncoming)
            {
                var creditsAlloc = Math.Min(amount + totalD, totalC);
                var debtsAlloc = creditsAlloc - amount;
                if (debtsAlloc < 0)
                    throw new BusinessRuleException("El cobro supera el saldo pendiente a tu favor en el evento");
                return (creditsAlloc, debtsAlloc);
            }
            if (isOutgoing)
            {
                var debtsAlloc = Math.Min(amount + totalC, totalD);
                var creditsAlloc = debtsAlloc - amount;
                if (creditsAlloc < 0)
                    throw new BusinessRuleException("El pago supera la deuda pendiente en el evento");
                return (creditsAlloc, debtsAlloc);
            }
            return (0, 0);
        }

        private static List<(SharedEventMovement Movement, SharedExpenseSplit Split, decimal Amount)> DistributeCredits(
            List<(SharedEventMovement Movement, SharedExpenseSplit Split)> items, decimal total)
        {
            var result = new List<(SharedEventMovement, SharedExpenseSplit, decimal)>();
            var remaining = total;
            foreach (var (movement, split) in items)
            {
                if (remaining <= 0) break;
                var pending = split.Amount - split.AmountReimbursed;
                var toApply = Math.Min(remaining, pending);
                if (toApply <= 0) continue;
                result.Add((movement, split, toApply));
                remaining -= toApply;
            }
            return result;
        }

        private static List<(SharedEventMovement Movement, SharedEventMovementShare Share, decimal Amount)> DistributeDebts(
            List<(SharedEventMovement Movement, SharedEventMovementShare Share)> items, decimal total)
        {
            var result = new List<(SharedEventMovement, SharedEventMovementShare, decimal)>();
            var remaining = total;
            foreach (var (movement, share) in items)
            {
                if (remaining <= 0) break;
                var pending = share.Amount - share.AmountSettled;
                var toApply = Math.Min(remaining, pending);
                if (toApply <= 0) continue;
                result.Add((movement, share, toApply));
                remaining -= toApply;
            }
            return result;
        }

        private static (List<(SharedEventMovement, SharedExpenseSplit, decimal)> Credits, List<(SharedEventMovement, SharedEventMovementShare, decimal)> Debts)
            ResolveManualAllocations(
                SharedEventPaymentAddDTO dto,
                List<(SharedEventMovement Movement, SharedExpenseSplit Split)> credits,
                List<(SharedEventMovement Movement, SharedEventMovementShare Share)> debts,
                bool isIncoming)
        {
            var creditResult = new List<(SharedEventMovement, SharedExpenseSplit, decimal)>();
            var debtResult = new List<(SharedEventMovement, SharedEventMovementShare, decimal)>();

            foreach (var alloc in dto.Allocations!)
            {
                var hasSplit = alloc.SplitId != null;
                var hasShare = alloc.ShareId != null;
                if (hasSplit == hasShare)
                    throw new BusinessRuleException("Cada allocation debe indicar exactamente un split o un share");
                if (alloc.Amount <= 0)
                    throw new BusinessRuleException("El monto de cada allocation debe ser mayor a cero");

                if (hasSplit)
                {
                    var found = credits.FirstOrDefault(c => c.Split.Id == alloc.SplitId!.Value);
                    if (found.Split == null)
                        throw new BusinessRuleException($"El split {alloc.SplitId} no tiene saldo pendiente en este evento/moneda");
                    var pending = found.Split.Amount - found.Split.AmountReimbursed;
                    if (alloc.Amount > pending)
                        throw new BusinessRuleException($"El monto asignado al split {alloc.SplitId} supera lo pendiente");
                    creditResult.Add((found.Movement, found.Split, alloc.Amount));
                }
                else
                {
                    var found = debts.FirstOrDefault(d => d.Share.Id == alloc.ShareId!.Value);
                    if (found.Share == null)
                        throw new BusinessRuleException($"El share {alloc.ShareId} no tiene deuda pendiente en este evento/moneda");
                    var pending = found.Share.Amount - found.Share.AmountSettled;
                    if (alloc.Amount > pending)
                        throw new BusinessRuleException($"El monto asignado al share {alloc.ShareId} supera lo pendiente");
                    debtResult.Add((found.Movement, found.Share, alloc.Amount));
                }
            }

            var creditsSum = creditResult.Sum(c => c.Item3);
            var debtsSum = debtResult.Sum(d => d.Item3);
            var expectedNet = dto.IsInternalCompensation ? 0 : (isIncoming ? dto.Amount : -dto.Amount);
            if (creditsSum - debtsSum != expectedNet)
                throw new BusinessRuleException("La imputación manual no cumple la ecuación del pago (créditos − deudas debe ser igual al efecto neto)");

            return (creditResult, debtResult);
        }

        // ── D3: aplicación por tipo de ítem ───────────────────────────────────

        private async Task<List<SharedEventPaymentAllocation>> ApplyCreditAsync(
            SharedEventMovement movement, SharedExpenseSplit split, decimal x, SharedEventPaymentAddDTO dto,
            Portfolio portfolio, int userId)
        {
            var sharedExpense = movement.SharedExpense!;
            var allocations = new List<SharedEventPaymentAllocation>();

            if (sharedExpense.TransactionId != null)
            {
                var transaction = await _transactionRepository.GetByIdAsync(sharedExpense.TransactionId.Value)
                    ?? throw new NotFoundException("Transacción no encontrada");

                transaction.Amount += x;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _transactionRepository.UpdateAsync(transaction);

                var allocation = new SharedEventPaymentAllocation
                {
                    SharedExpenseSplitId = split.Id,
                    Amount = x,
                    TouchedTransactionId = transaction.Id
                };

                if (transaction.AccountId != dto.AccountId!.Value)
                {
                    var (exOutId, exInId) = await CreateExchangePairAsync(transaction.AccountId, dto.AccountId.Value, transaction.AssetId, dto.Date, x, portfolio.Id, userId);
                    allocation.CreatedExchangeOutTransactionId = exOutId;
                    allocation.CreatedExchangeInTransactionId = exInId;
                }

                split.AmountReimbursed += x;
                allocations.Add(allocation);
            }
            else
            {
                var cardTransactionId = sharedExpense.CardTransactionId!.Value;
                var paidInstallments = (await _transactionRepository.GetByCardTransactionIdAsync(cardTransactionId)).ToList();

                var directCap = Math.Max(0, paidInstallments.Count * split.InstallmentSplitAmount - split.AmountApplied);
                var directAmount = Math.Min(x, directCap);
                var futureAmount = x - directAmount;

                if (directAmount > 0)
                {
                    var remaining = directAmount;
                    var alreadyAppliedGlobal = split.AmountApplied;
                    for (var i = 0; i < paidInstallments.Count && remaining > 0; i++)
                    {
                        var budgetUsed = Math.Clamp(alreadyAppliedGlobal - i * split.InstallmentSplitAmount, 0, split.InstallmentSplitAmount);
                        var headroom = split.InstallmentSplitAmount - budgetUsed;
                        if (headroom <= 0) continue;

                        var toApply = Math.Min(remaining, headroom);
                        var installmentTx = paidInstallments[i];
                        installmentTx.Amount += toApply;
                        installmentTx.UpdatedAt = DateTime.UtcNow;
                        await _transactionRepository.UpdateAsync(installmentTx);

                        var tramoAllocation = new SharedEventPaymentAllocation
                        {
                            SharedExpenseSplitId = split.Id,
                            Amount = toApply,
                            TouchedTransactionId = installmentTx.Id
                        };

                        if (installmentTx.AccountId != dto.AccountId!.Value)
                        {
                            var (exOutId, exInId) = await CreateExchangePairAsync(installmentTx.AccountId, dto.AccountId.Value, installmentTx.AssetId, dto.Date, toApply, portfolio.Id, userId);
                            tramoAllocation.CreatedExchangeOutTransactionId = exOutId;
                            tramoAllocation.CreatedExchangeInTransactionId = exInId;
                        }

                        allocations.Add(tramoAllocation);
                        remaining -= toApply;
                    }

                    split.AmountApplied += directAmount;
                    split.AmountReimbursed += directAmount;
                }

                if (futureAmount > 0)
                {
                    var cardTransaction = await _cardTransactionRepository.GetByIdAsync(cardTransactionId)
                        ?? throw new NotFoundException("Gasto de tarjeta no encontrado");
                    var reintegroClass = await _transactionClassRepository.GetTransactionClassByDescriptionAsync("Reintegro", userId)
                        ?? throw new NotFoundException("Clase de transacción 'Reintegro' no encontrada");

                    var placeholder = await _transactionRepository.AddAsyncReturnObject(new Transaction
                    {
                        AccountId = dto.AccountId!.Value,
                        PortfolioId = portfolio.Id,
                        AssetId = cardTransaction.AssetId,
                        Date = dto.Date,
                        MovementType = "I",
                        TransactionClassId = reintegroClass.Id,
                        Detail = $"Reintegro - {cardTransaction.Detail}",
                        Amount = futureAmount,
                        UserId = userId
                    });

                    await _sharedExpenseRepository.AddReimbursementAsync(new SharedExpenseReimbursement
                    {
                        SharedExpenseSplitId = split.Id,
                        TransactionId = placeholder.Id,
                        Amount = futureAmount,
                        Date = dto.Date
                    });

                    split.AmountReimbursed += futureAmount;

                    allocations.Add(new SharedEventPaymentAllocation
                    {
                        SharedExpenseSplitId = split.Id,
                        Amount = futureAmount,
                        CreatedIncomeTransactionId = placeholder.Id
                    });
                }
            }

            split.Status = ComputeSplitStatus(split);
            split.UpdatedAt = DateTime.UtcNow;
            await _sharedExpenseRepository.UpdateSplitAsync(split);

            return allocations;
        }

        private async Task<SharedEventPaymentAllocation> ApplyDebtAsync(
            SharedEventMovement movement, SharedEventMovementShare share, decimal x, SharedEventPaymentAddDTO dto,
            string eventName, Portfolio portfolio, int userId)
        {
            var expenseTransaction = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = dto.AccountId!.Value,
                PortfolioId = portfolio.Id,
                AssetId = movement.AssetId,
                Date = dto.Date,
                MovementType = "E",
                TransactionClassId = movement.TransactionClassId,
                Detail = $"(Evento: {eventName}) {movement.Description}",
                Amount = -x,
                UserId = userId
            });

            share.AmountSettled += x;
            share.UpdatedAt = DateTime.UtcNow;
            await _sharedEventMovementRepository.UpdateShareAsync(share);

            return new SharedEventPaymentAllocation
            {
                SharedEventMovementShareId = share.Id,
                Amount = x,
                CreatedExpenseTransactionId = expenseTransaction.Id
            };
        }

        private async Task<(int ExOutId, int ExInId)> CreateExchangePairAsync(
            int fromAccountId, int toAccountId, int assetId, DateTime date, decimal amount, int portfolioId, int userId)
        {
            var time = DateTime.UtcNow;
            var exOut = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = fromAccountId,
                PortfolioId = portfolioId,
                AssetId = assetId,
                Date = date,
                MovementType = "EX",
                TransactionClassId = null,
                Detail = "Evento compartido",
                Amount = -amount,
                UserId = userId,
                CreatedAt = time,
                UpdatedAt = time
            });
            var exIn = await _transactionRepository.AddAsyncReturnObject(new Transaction
            {
                AccountId = toAccountId,
                PortfolioId = portfolioId,
                AssetId = assetId,
                Date = date,
                MovementType = "EX",
                TransactionClassId = null,
                Detail = "Evento compartido",
                Amount = amount,
                UserId = userId,
                CreatedAt = time,
                UpdatedAt = time
            });
            return (exOut.Id, exIn.Id);
        }

        private static SharedExpenseSplitStatus ComputeSplitStatus(SharedExpenseSplit split)
        {
            if (split.AmountReimbursed >= split.Amount) return SharedExpenseSplitStatus.Paid;
            return split.AmountReimbursed > 0 ? SharedExpenseSplitStatus.PartiallyPaid : SharedExpenseSplitStatus.Pending;
        }

        // ── Reversa ───────────────────────────────────────────────────────────

        private async Task ReverseAllocationAsync(SharedEventPaymentAllocation allocation)
        {
            if (allocation.SharedExpenseSplitId != null)
            {
                var split = await _sharedExpenseRepository.GetSplitByIdAsync(allocation.SharedExpenseSplitId.Value)
                    ?? throw new NotFoundException("Split no encontrado al revertir el pago");

                split.AmountReimbursed -= allocation.Amount;

                if (allocation.CreatedIncomeTransactionId != null)
                {
                    var reimbursements = await _sharedExpenseRepository.GetReimbursementsBySplitIdAsync(split.Id);
                    var reimbursement = reimbursements.FirstOrDefault(r => r.TransactionId == allocation.CreatedIncomeTransactionId.Value);
                    if (reimbursement != null)
                        await _sharedExpenseRepository.DeleteReimbursementAsync(reimbursement.Id);
                    await _transactionRepository.DeleteAsync(allocation.CreatedIncomeTransactionId.Value);
                }
                else
                {
                    if (split.SharedExpense?.CardTransactionId != null)
                        split.AmountApplied -= allocation.Amount;

                    if (allocation.TouchedTransactionId != null)
                    {
                        var touched = await _transactionRepository.GetByIdAsync(allocation.TouchedTransactionId.Value);
                        if (touched != null)
                        {
                            touched.Amount -= allocation.Amount;
                            touched.UpdatedAt = DateTime.UtcNow;
                            await _transactionRepository.UpdateAsync(touched);
                        }
                    }

                    if (allocation.CreatedExchangeOutTransactionId != null)
                        await _transactionRepository.DeleteAsync(allocation.CreatedExchangeOutTransactionId.Value);
                    if (allocation.CreatedExchangeInTransactionId != null)
                        await _transactionRepository.DeleteAsync(allocation.CreatedExchangeInTransactionId.Value);
                }

                split.Status = ComputeSplitStatus(split);
                split.UpdatedAt = DateTime.UtcNow;
                await _sharedExpenseRepository.UpdateSplitAsync(split);
            }
            else if (allocation.SharedEventMovementShareId != null)
            {
                var share = await _sharedEventMovementRepository.GetShareByIdAsync(allocation.SharedEventMovementShareId.Value)
                    ?? throw new NotFoundException("Share no encontrado al revertir el pago");

                share.AmountSettled -= allocation.Amount;
                share.UpdatedAt = DateTime.UtcNow;
                await _sharedEventMovementRepository.UpdateShareAsync(share);

                if (allocation.CreatedExpenseTransactionId != null)
                    await _transactionRepository.DeleteAsync(allocation.CreatedExpenseTransactionId.Value);
            }
        }

        // ── Validaciones y utilidades ─────────────────────────────────────────

        private static bool IsThirdPartyOnly(SharedEventPaymentAddDTO dto) =>
            dto.FromPersonId != null && dto.ToPersonId != null && !dto.IsInternalCompensation;

        private static void ValidatePaymentInput(SharedEvent sharedEvent, SharedEventPaymentAddDTO dto)
        {
            var fromIsUser = dto.FromPersonId == null;
            var toIsUser = dto.ToPersonId == null;

            if (fromIsUser && toIsUser && !dto.IsInternalCompensation)
                throw new BusinessRuleException("Un pago entre el usuario y sí mismo no es válido (usar compensación interna)");

            if (!fromIsUser && !toIsUser && dto.FromPersonId == dto.ToPersonId)
                throw new BusinessRuleException("El origen y destino del pago no pueden ser la misma persona");

            var participantIds = sharedEvent.Participants.Select(p => p.PersonId).ToHashSet();
            if (dto.FromPersonId != null && !participantIds.Contains(dto.FromPersonId.Value))
                throw new BusinessRuleException("La persona de origen no es participante del evento");
            if (dto.ToPersonId != null && !participantIds.Contains(dto.ToPersonId.Value))
                throw new BusinessRuleException("La persona de destino no es participante del evento");

            if (dto.IsInternalCompensation)
            {
                if (dto.FromPersonId != null || dto.ToPersonId != null)
                    throw new BusinessRuleException("La compensación interna no debe indicar origen ni destino");
                if (dto.Amount != 0)
                    throw new BusinessRuleException("La compensación interna debe tener monto 0");
                if (dto.AccountId == null)
                    throw new BusinessRuleException("La compensación interna requiere una cuenta pivote");
            }
            else
            {
                if (dto.Amount <= 0)
                    throw new BusinessRuleException("El monto del pago debe ser mayor a cero");

                var involvesUser = fromIsUser || toIsUser;
                if (involvesUser && dto.AccountId == null)
                    throw new BusinessRuleException("Debe indicar la cuenta del pago");
            }
        }

        private async Task<SharedEvent> GetOwnedEventAsync(int userId, int id)
        {
            var sharedEvent = await _sharedEventRepository.GetDetailByIdAsync(id)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            return sharedEvent;
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
    }
}
