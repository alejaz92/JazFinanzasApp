using System.Globalization;
using System.Text;
using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.DTO.SharedEvent.Import;
using JazFinanzasApp.API.Business.DTO.SharedExpense;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class SharedEventImportService : ISharedEventImportService
    {
        private const decimal SuggestedMatchAmountTolerance = 0.5m;
        private const int SuggestedMatchDayTolerance = 3;

        private readonly ISharedEventRepository _sharedEventRepository;
        private readonly ISharedEventService _sharedEventService;
        private readonly ISharedEventPaymentService _sharedEventPaymentService;
        private readonly ISharedEventMovementRepository _sharedEventMovementRepository;
        private readonly ISharedExpenseService _sharedExpenseService;
        private readonly ISharedExpenseRepository _sharedExpenseRepository;
        private readonly IPersonRepository _personRepository;
        private readonly ITransactionClassRepository _transactionClassRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SharedEventImportService(
            ISharedEventRepository sharedEventRepository,
            ISharedEventService sharedEventService,
            ISharedEventPaymentService sharedEventPaymentService,
            ISharedEventMovementRepository sharedEventMovementRepository,
            ISharedExpenseService sharedExpenseService,
            ISharedExpenseRepository sharedExpenseRepository,
            IPersonRepository personRepository,
            ITransactionClassRepository transactionClassRepository,
            IAssetRepository assetRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            IUnitOfWork unitOfWork)
        {
            _sharedEventRepository = sharedEventRepository;
            _sharedEventService = sharedEventService;
            _sharedEventPaymentService = sharedEventPaymentService;
            _sharedEventMovementRepository = sharedEventMovementRepository;
            _sharedExpenseService = sharedExpenseService;
            _sharedExpenseRepository = sharedExpenseRepository;
            _personRepository = personRepository;
            _transactionClassRepository = transactionClassRepository;
            _assetRepository = assetRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _unitOfWork = unitOfWork;
        }

        // ── Parse ─────────────────────────────────────────────────────────────

        public async Task<SharedEventImportParseResultDTO> ParseAsync(int userId, SharedEventImportParseDTO dto)
        {
            var parsed = SplitwiseCsvParser.Parse(dto.CsvContent);

            var existingPeople = (await _personRepository.GetByUserIdAsync(userId)).ToList();
            var existingClasses = (await _transactionClassRepository.GetByUserIdAsync(userId)).Where(c => c.IncExp == "E").ToList();
            var assets = (await _assetRepository.GetAssetsAsync()).ToList();

            var memberDtos = parsed.MemberNames.Select(name =>
            {
                var match = FindBestMatch(name, existingPeople, p => p.Name, p => p.Alias);
                return new SharedEventImportMemberDTO
                {
                    Name = name,
                    SuggestedPersonId = match?.Id,
                    SuggestedPersonName = match == null ? null : (match.Alias ?? match.Name)
                };
            }).ToList();

            var categoryDtos = parsed.CategoryNames.Select(name =>
            {
                var match = FindBestMatch(name, existingClasses, c => c.Description, c => null);
                return new SharedEventImportCategoryDTO
                {
                    Name = name,
                    SuggestedTransactionClassId = match?.Id,
                    SuggestedTransactionClassName = match?.Description
                };
            }).ToList();

            var warnings = new List<string>();
            var rowDtos = new List<SharedEventImportRowDTO>();

            foreach (var row in parsed.Rows)
            {
                if (row.Unsupported)
                    warnings.Add($"Fila {row.RowIndex + 1} ('{row.Description}'): no tiene un único pagador (gasto) o pagador/receptor (pago) identificable — requiere revisión manual.");

                var asset = assets.FirstOrDefault(a => a.Symbol.Equals(row.Currency, StringComparison.OrdinalIgnoreCase));
                if (asset == null)
                    warnings.Add($"Fila {row.RowIndex + 1}: no se encontró la moneda '{row.Currency}' entre los activos existentes.");

                var suggestedMatches = new List<SharedEventImportSuggestedMatchDTO>();
                if (!row.Unsupported && !row.IsPayment && asset != null
                    && dto.CurrentUserMemberName != null && row.PayerMemberName == dto.CurrentUserMemberName)
                {
                    suggestedMatches = await FindSuggestedMatchesAsync(userId, asset.Id, row.Cost, row.Date);
                }

                rowDtos.Add(new SharedEventImportRowDTO
                {
                    RowIndex = row.RowIndex,
                    Date = row.Date,
                    Description = row.Description,
                    Category = row.Category,
                    Cost = row.Cost,
                    Currency = row.Currency,
                    AssetId = asset?.Id,
                    IsPayment = row.IsPayment,
                    Unsupported = row.Unsupported,
                    PayerMemberName = row.PayerMemberName,
                    ReceiverMemberName = row.ReceiverMemberName,
                    MemberDeltas = row.MemberDeltas.Select(d => new SharedEventImportMemberDeltaDTO { MemberName = d.MemberName, Delta = d.Delta }).ToList(),
                    SuggestedMatches = suggestedMatches
                });
            }

            foreach (var balanceRow in parsed.BalanceRows)
            {
                foreach (var memberBalance in balanceRow.MemberBalances)
                {
                    var accumulated = parsed.Rows
                        .Where(r => r.Currency.Equals(balanceRow.Currency, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(r => r.MemberDeltas)
                        .Where(d => d.MemberName == memberBalance.MemberName)
                        .Sum(d => d.Delta);

                    if (Math.Abs(accumulated - memberBalance.Delta) > 0.05m)
                        warnings.Add($"El saldo acumulado calculado para {memberBalance.MemberName} en {balanceRow.Currency} ({accumulated:0.00}) no coincide con el 'Saldo total' del archivo ({memberBalance.Delta:0.00}).");
                }
            }

            return new SharedEventImportParseResultDTO
            {
                Members = memberDtos,
                Categories = categoryDtos,
                Rows = rowDtos,
                BalanceRows = parsed.BalanceRows.Select(b => new SharedEventImportBalanceRowDTO
                {
                    Currency = b.Currency,
                    MemberBalances = b.MemberBalances.Select(d => new SharedEventImportMemberDeltaDTO { MemberName = d.MemberName, Delta = d.Delta }).ToList()
                }).ToList(),
                Warnings = warnings
            };
        }

        private async Task<List<SharedEventImportSuggestedMatchDTO>> FindSuggestedMatchesAsync(int userId, int assetId, decimal amount, DateTime date)
        {
            var result = new List<SharedEventImportSuggestedMatchDTO>();
            var fromDate = date.AddDays(-SuggestedMatchDayTolerance);
            var toDate = date.AddDays(SuggestedMatchDayTolerance);

            var transactions = await _transactionRepository.FindAsync(t =>
                t.UserId == userId && t.MovementType == "E" && t.AssetId == assetId && t.CardTransactionId == null
                && t.Date >= fromDate && t.Date <= toDate
                && (t.Amount + amount <= SuggestedMatchAmountTolerance && t.Amount + amount >= -SuggestedMatchAmountTolerance));

            foreach (var t in transactions)
            {
                if (await _sharedExpenseRepository.GetByTransactionIdAsync(t.Id) != null) continue;
                result.Add(new SharedEventImportSuggestedMatchDTO { TransactionId = t.Id, Date = t.Date, Amount = Math.Abs(t.Amount), Detail = t.Detail });
            }

            var cardTransactions = await _cardTransactionRepository.FindAsync(ct =>
                ct.UserId == userId && ct.AssetId == assetId
                && ct.Date >= fromDate && ct.Date <= toDate
                && (ct.TotalAmount - amount <= SuggestedMatchAmountTolerance && ct.TotalAmount - amount >= -SuggestedMatchAmountTolerance));

            foreach (var ct in cardTransactions)
            {
                if (await _sharedExpenseRepository.GetByCardTransactionIdAsync(ct.Id) != null) continue;
                result.Add(new SharedEventImportSuggestedMatchDTO { CardTransactionId = ct.Id, Date = ct.Date, Amount = ct.TotalAmount, Detail = ct.Detail });
            }

            return result;
        }

        private static T? FindBestMatch<T>(string csvName, List<T> candidates, Func<T, string> nameSelector, Func<T, string?> aliasSelector) where T : class
        {
            var normalized = Normalize(csvName);

            var exact = candidates.FirstOrDefault(c => Normalize(nameSelector(c)) == normalized || Normalize(aliasSelector(c) ?? string.Empty) == normalized);
            if (exact != null) return exact;

            return candidates.FirstOrDefault(c =>
            {
                var n = Normalize(nameSelector(c));
                return n.Length > 0 && (n.Contains(normalized) || normalized.Contains(n));
            });
        }

        private static string Normalize(string s)
        {
            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in formD)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
        }

        // ── Confirm ───────────────────────────────────────────────────────────

        public async Task<SharedEventImportConfirmResultDTO> ConfirmAsync(int userId, int sharedEventId, SharedEventImportConfirmDTO dto)
        {
            var sharedEvent = await _sharedEventRepository.GetDetailByIdAsync(sharedEventId)
                ?? throw new NotFoundException("Evento compartido no encontrado");
            if (sharedEvent.UserId != userId) throw new UnauthorizedDomainException();
            if (sharedEvent.IsClosed) throw new BusinessRuleException("El evento está cerrado");

            var parsed = SplitwiseCsvParser.Parse(dto.CsvContent);

            var memberToPersonId = await ResolveMemberMappingsAsync(userId, sharedEventId, dto.MemberMappings);
            var missingMembers = parsed.MemberNames.Where(m => !memberToPersonId.ContainsKey(m)).ToList();
            if (missingMembers.Any())
                throw new BusinessRuleException($"Faltan mapeos de miembro para: {string.Join(", ", missingMembers)}");

            var categoryToClassId = await ResolveCategoryMappingsAsync(userId, dto.CategoryMappings);
            var assets = (await _assetRepository.GetAssetsAsync()).ToList();
            var rowDecisions = dto.RowDecisions.ToDictionary(r => r.RowIndex);

            var result = new SharedEventImportConfirmResultDTO();

            foreach (var row in parsed.Rows)
            {
                if (!rowDecisions.TryGetValue(row.RowIndex, out var decision) || decision.Action == SharedEventImportRowAction.Skip)
                {
                    result.Skipped++;
                    continue;
                }

                if (row.Unsupported)
                {
                    result.Errors.Add($"Fila {row.RowIndex + 1} ('{row.Description}'): no soportada (más de un pagador o receptor), se saltea");
                    result.Skipped++;
                    continue;
                }

                var asset = assets.FirstOrDefault(a => a.Symbol.Equals(row.Currency, StringComparison.OrdinalIgnoreCase));
                if (asset == null)
                {
                    result.Errors.Add($"Fila {row.RowIndex + 1}: no se encontró la moneda '{row.Currency}'");
                    result.Skipped++;
                    continue;
                }

                try
                {
                    if (row.IsPayment)
                    {
                        await ImportPaymentRowAsync(userId, sharedEventId, row, asset.Id, memberToPersonId, decision);
                        result.PaymentsCreated++;
                    }
                    else if (decision.Action == SharedEventImportRowAction.LinkExisting)
                    {
                        await ImportMovementFromExistingAsync(userId, sharedEventId, row, asset.Id, categoryToClassId, memberToPersonId, decision);
                        result.MovementsCreated++;
                    }
                    else
                    {
                        await ImportMovementAsNewAsync(userId, sharedEventId, row, asset.Id, categoryToClassId, memberToPersonId, decision);
                        result.MovementsCreated++;
                    }
                }
                catch (Exception ex) when (ex is BusinessRuleException or NotFoundException or UnauthorizedDomainException)
                {
                    result.Errors.Add($"Fila {row.RowIndex + 1} ('{row.Description}'): {ex.Message}");
                    result.Skipped++;
                }
            }

            return result;
        }

        private async Task<Dictionary<string, int?>> ResolveMemberMappingsAsync(int userId, int sharedEventId, List<SharedEventImportMemberMappingDTO> mappings)
        {
            var currentUserMappings = mappings.Where(m => m.IsCurrentUser).ToList();
            if (currentUserMappings.Count != 1)
                throw new BusinessRuleException("Debe indicarse exactamente un miembro del archivo como \"soy yo\"");

            var result = new Dictionary<string, int?>();

            foreach (var mapping in mappings)
            {
                if (mapping.IsCurrentUser)
                {
                    result[mapping.MemberName] = null;
                    continue;
                }

                if (mapping.PersonId.HasValue == !string.IsNullOrWhiteSpace(mapping.NewPersonName))
                    throw new BusinessRuleException($"El miembro '{mapping.MemberName}' debe mapearse a una persona existente o a una nueva, no ambas ni ninguna");

                int personId;
                if (mapping.PersonId.HasValue)
                {
                    var person = await _personRepository.GetByIdAsync(mapping.PersonId.Value)
                        ?? throw new NotFoundException($"Persona {mapping.PersonId} no encontrada");
                    if (person.UserId != userId) throw new UnauthorizedDomainException();
                    personId = person.Id;
                }
                else
                {
                    var created = await _personRepository.AddAsyncReturnObject(new Person { Name = mapping.NewPersonName!, UserId = userId });
                    personId = created.Id;
                }

                result[mapping.MemberName] = personId;

                if (await _sharedEventRepository.GetParticipantAsync(sharedEventId, personId) == null)
                    await _sharedEventRepository.AddParticipantAsync(new SharedEventParticipant { SharedEventId = sharedEventId, PersonId = personId });
            }

            return result;
        }

        private async Task<Dictionary<string, int>> ResolveCategoryMappingsAsync(int userId, List<SharedEventImportCategoryMappingDTO> mappings)
        {
            var result = new Dictionary<string, int>();

            foreach (var mapping in mappings)
            {
                if (mapping.TransactionClassId.HasValue == !string.IsNullOrWhiteSpace(mapping.NewCategoryName))
                    throw new BusinessRuleException($"La categoría '{mapping.CategoryName}' debe mapearse a una existente o a una nueva, no ambas ni ninguna");

                int classId;
                if (mapping.TransactionClassId.HasValue)
                {
                    var tc = await _transactionClassRepository.GetByIdAsync(mapping.TransactionClassId.Value)
                        ?? throw new NotFoundException($"Categoría {mapping.TransactionClassId} no encontrada");
                    if (tc.UserId != userId) throw new UnauthorizedDomainException();
                    if (tc.IncExp == "I") throw new BusinessRuleException($"La categoría '{tc.Description}' debe ser de egreso");
                    classId = tc.Id;
                }
                else
                {
                    var created = await _transactionClassRepository.AddAsyncReturnObject(new TransactionClass { Description = mapping.NewCategoryName!, IncExp = "E", UserId = userId });
                    classId = created.Id;
                }

                result[mapping.CategoryName] = classId;
            }

            return result;
        }

        private async Task ImportPaymentRowAsync(int userId, int sharedEventId, SplitwiseRow row, int assetId, Dictionary<string, int?> memberToPersonId, SharedEventImportRowDecisionDTO decision)
        {
            var fromPersonId = memberToPersonId[row.PayerMemberName!];
            var toPersonId = memberToPersonId[row.ReceiverMemberName!];
            var involvesUser = fromPersonId == null || toPersonId == null;

            if (involvesUser && decision.AccountId == null)
                throw new BusinessRuleException("Debe indicar la cuenta para este pago");

            await _sharedEventPaymentService.CreatePaymentAsync(userId, sharedEventId, new SharedEventPaymentAddDTO
            {
                Date = row.Date,
                AssetId = assetId,
                Amount = row.Cost,
                FromPersonId = fromPersonId,
                ToPersonId = toPersonId,
                AccountId = involvesUser ? decision.AccountId : null,
                IsInternalCompensation = false,
                Notes = "Importado de Splitwise"
            });
        }

        private async Task ImportMovementAsNewAsync(int userId, int sharedEventId, SplitwiseRow row, int assetId, Dictionary<string, int> categoryToClassId, Dictionary<string, int?> memberToPersonId, SharedEventImportRowDecisionDTO decision)
        {
            var payerPersonId = memberToPersonId[row.PayerMemberName!];
            var shares = row.ComputeShares();

            var shareInputs = shares.Select(kv => new SharedEventMovementShareInputDTO
            {
                PersonId = memberToPersonId[kv.Key],
                Amount = kv.Value
            }).ToList();

            SharedEventMovementPaymentInputDTO? payment = null;
            if (payerPersonId == null)
            {
                if (decision.AccountId == null)
                    throw new BusinessRuleException("Debe indicar la cuenta con la que se pagó este gasto");
                payment = new SharedEventMovementPaymentInputDTO { AccountId = decision.AccountId };
            }

            await _sharedEventService.CreateMovementAsync(userId, sharedEventId, new SharedEventMovementAddDTO
            {
                Date = row.Date,
                Description = row.Description,
                TransactionClassId = categoryToClassId[row.Category],
                AssetId = assetId,
                TotalAmount = row.Cost,
                PayerPersonId = payerPersonId,
                Shares = shareInputs,
                Payment = payment,
                Notes = "Importado de Splitwise"
            });
        }

        // Vincula el movimiento a una Transaction/CardTransaction ya existente (ej. cargada manualmente antes de importar),
        // reutilizando el motor V1 (ISharedExpenseService) para no duplicar la lógica de creación de splits.
        private async Task ImportMovementFromExistingAsync(int userId, int sharedEventId, SplitwiseRow row, int assetId, Dictionary<string, int> categoryToClassId, Dictionary<string, int?> memberToPersonId, SharedEventImportRowDecisionDTO decision)
        {
            if (decision.TransactionId == null && decision.CardTransactionId == null)
                throw new BusinessRuleException("Debe indicar la transacción existente a vincular");

            var payerPersonId = memberToPersonId[row.PayerMemberName!];
            if (payerPersonId != null)
                throw new BusinessRuleException("Vincular a una transacción existente solo aplica a filas pagadas por vos");

            var shares = row.ComputeShares();
            var thirdPartyShares = shares.Where(kv => memberToPersonId[kv.Key] != null).ToList();
            if (!thirdPartyShares.Any())
                throw new BusinessRuleException("No hay participantes de terceros para compartir en esta fila");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var sharedExpense = await _sharedExpenseService.CreateAsync(userId, new SharedExpenseAddDTO
                {
                    TransactionId = decision.TransactionId,
                    CardTransactionId = decision.CardTransactionId,
                    Notes = "Importado de Splitwise",
                    Splits = thirdPartyShares.Select(kv => new SplitInputDTO { PersonId = memberToPersonId[kv.Key]!.Value, Amount = kv.Value }).ToList()
                });

                var movement = new SharedEventMovement
                {
                    SharedEventId = sharedEventId,
                    Date = row.Date,
                    Description = row.Description,
                    TransactionClassId = categoryToClassId[row.Category],
                    AssetId = assetId,
                    TotalAmount = row.Cost,
                    PayerPersonId = null,
                    TransactionId = decision.TransactionId,
                    CardTransactionId = decision.CardTransactionId,
                    SharedExpenseId = sharedExpense.Id,
                    Notes = "Importado de Splitwise (vinculado a movimiento existente)",
                    UserId = userId,
                    Shares = shares.Select(kv => new SharedEventMovementShare
                    {
                        PersonId = memberToPersonId[kv.Key],
                        Amount = kv.Value,
                        AmountSettled = 0,
                        SharedExpenseSplitId = memberToPersonId[kv.Key] != null
                            ? sharedExpense.Splits.First(s => s.PersonId == memberToPersonId[kv.Key]!.Value).Id
                            : (int?)null
                    }).ToList()
                };

                await _sharedEventMovementRepository.AddAsyncReturnObject(movement);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
