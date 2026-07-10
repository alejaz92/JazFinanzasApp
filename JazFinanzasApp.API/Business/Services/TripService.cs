using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class TripService : ITripService
    {
        private static readonly string[] ValidTypes = { "DOMESTIC", "INTERNATIONAL" };

        private const string MovementTypeAccount = "ACCOUNT";
        private const string MovementTypeCard = "CARD";

        private readonly ITripRepository _tripRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly ITripSuggestionDismissalRepository _dismissalRepository;

        public TripService(
            ITripRepository tripRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            ITripSuggestionDismissalRepository dismissalRepository)
        {
            _tripRepository = tripRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _dismissalRepository = dismissalRepository;
        }

        public async Task<IEnumerable<TripDTO>> GetAllForUserAsync(int userId)
        {
            var trips = await _tripRepository.GetByUserIdAsync(userId);
            return trips.Select(MapToDTO);
        }

        public async Task<TripDetailDTO> GetByIdAsync(int userId, int id)
        {
            var trip = await GetOwnedTripAsync(userId, id);

            var transactions = await _transactionRepository.GetTransactionsByTripIdAsync(id);
            var cardTransactions = await _cardTransactionRepository.GetCardTransactionsByTripIdAsync(id);

            var detail = new TripDetailDTO
            {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Status = GetStatus(trip),
                Movements = transactions.Select(MapAccountMovement)
                    .Concat(cardTransactions.Select(MapCardMovement))
                    .OrderBy(m => m.Date)
                    .ToList()
            };
            return detail;
        }

        public async Task<TripDTO> CreateTripAsync(int userId, TripAddDTO dto)
        {
            ValidateFields(dto.Type, dto.StartDate, dto.EndDate);

            var existing = await _tripRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId);
            if (existing.Any()) throw new BusinessRuleException("Ya existe un viaje con ese nombre");

            var trip = new Trip
            {
                Name = dto.Name,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                UserId = userId
            };
            var created = await _tripRepository.AddAsyncReturnObject(trip);
            return MapToDTO(created);
        }

        public async Task UpdateTripAsync(int userId, int id, TripEditDTO dto)
        {
            var trip = await GetOwnedTripAsync(userId, id);

            ValidateFields(dto.Type, dto.StartDate, dto.EndDate);

            var duplicate = await _tripRepository.FindAsync(t => t.Name == dto.Name && t.UserId == userId && t.Id != id);
            if (duplicate.Any()) throw new BusinessRuleException("Ya existe un viaje con ese nombre");

            trip.Name = dto.Name;
            trip.Type = dto.Type;
            trip.StartDate = dto.StartDate;
            trip.EndDate = dto.EndDate;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip);
        }

        public async Task DeleteTripAsync(int userId, int id)
        {
            await GetOwnedTripAsync(userId, id);

            // Los movimientos quedan desasociados por ON DELETE SET NULL y los descartes se borran por cascade
            await _tripRepository.DeleteAsync(id);
        }

        public async Task AssociateMovementsAsync(int userId, int tripId, TripAssociationsDTO dto)
        {
            await GetOwnedTripAsync(userId, tripId);

            foreach (var movementRef in dto.Movements)
            {
                if (movementRef.Type == MovementTypeAccount)
                {
                    var transaction = await GetOwnedTransactionAsync(userId, movementRef.Id);
                    if (transaction.TripId == tripId) continue;
                    if (transaction.TripId != null)
                        throw new BusinessRuleException("El movimiento ya pertenece a otro viaje");
                    EnsureTransactionIsAssociable(transaction);

                    transaction.TripId = tripId;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _transactionRepository.UpdateAsync(transaction);
                }
                else if (movementRef.Type == MovementTypeCard)
                {
                    var cardTransaction = await GetOwnedCardTransactionAsync(userId, movementRef.Id);
                    if (cardTransaction.TripId == tripId) continue;
                    if (cardTransaction.TripId != null)
                        throw new BusinessRuleException("El movimiento ya pertenece a otro viaje");

                    cardTransaction.TripId = tripId;
                    cardTransaction.UpdatedAt = DateTime.UtcNow;
                    await _cardTransactionRepository.UpdateAsync(cardTransaction);
                }
                else
                {
                    throw new BusinessRuleException("Tipo de movimiento inválido");
                }

                // Si estaba descartado como sugerencia, el descarte deja de tener sentido
                await DeleteDismissalIfExistsAsync(tripId, movementRef);
            }
        }

        public async Task DisassociateMovementsAsync(int userId, int tripId, TripAssociationsDTO dto)
        {
            await GetOwnedTripAsync(userId, tripId);

            foreach (var movementRef in dto.Movements)
            {
                if (movementRef.Type == MovementTypeAccount)
                {
                    var transaction = await GetOwnedTransactionAsync(userId, movementRef.Id);
                    if (transaction.TripId != tripId)
                        throw new BusinessRuleException("El movimiento no pertenece a este viaje");

                    transaction.TripId = null;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _transactionRepository.UpdateAsync(transaction);
                }
                else if (movementRef.Type == MovementTypeCard)
                {
                    var cardTransaction = await GetOwnedCardTransactionAsync(userId, movementRef.Id);
                    if (cardTransaction.TripId != tripId)
                        throw new BusinessRuleException("El movimiento no pertenece a este viaje");

                    cardTransaction.TripId = null;
                    cardTransaction.UpdatedAt = DateTime.UtcNow;
                    await _cardTransactionRepository.UpdateAsync(cardTransaction);
                }
                else
                {
                    throw new BusinessRuleException("Tipo de movimiento inválido");
                }
            }
        }

        public async Task<IEnumerable<TripMovementDTO>> GetSuggestionsAsync(int userId, int tripId)
        {
            var trip = await GetOwnedTripAsync(userId, tripId);

            var dismissals = await _dismissalRepository.GetByTripIdAsync(tripId);
            var dismissedTransactionIds = dismissals
                .Where(d => d.TransactionId != null)
                .Select(d => d.TransactionId!.Value)
                .ToHashSet();
            var dismissedCardTransactionIds = dismissals
                .Where(d => d.CardTransactionId != null)
                .Select(d => d.CardTransactionId!.Value)
                .ToHashSet();

            var transactions = await _transactionRepository
                .GetTripSuggestibleTransactionsAsync(userId, trip.StartDate, trip.EndDate);
            var cardTransactions = await _cardTransactionRepository
                .GetTripSuggestibleCardTransactionsAsync(userId, trip.StartDate, trip.EndDate);

            return transactions
                .Where(t => !dismissedTransactionIds.Contains(t.Id))
                .Select(MapAccountMovement)
                .Concat(cardTransactions
                    .Where(ct => !dismissedCardTransactionIds.Contains(ct.Id))
                    .Select(MapCardMovement))
                .OrderBy(m => m.Date)
                .ToList();
        }

        public async Task DismissSuggestionAsync(int userId, int tripId, TripMovementRefDTO dto)
        {
            await GetOwnedTripAsync(userId, tripId);

            int? transactionId = null;
            int? cardTransactionId = null;

            if (dto.Type == MovementTypeAccount)
            {
                var transaction = await GetOwnedTransactionAsync(userId, dto.Id);
                if (transaction.TripId != null)
                    throw new BusinessRuleException("El movimiento ya está asociado a un viaje");
                EnsureTransactionIsAssociable(transaction);
                transactionId = dto.Id;
            }
            else if (dto.Type == MovementTypeCard)
            {
                var cardTransaction = await GetOwnedCardTransactionAsync(userId, dto.Id);
                if (cardTransaction.TripId != null)
                    throw new BusinessRuleException("El movimiento ya está asociado a un viaje");
                cardTransactionId = dto.Id;
            }
            else
            {
                throw new BusinessRuleException("Tipo de movimiento inválido");
            }

            var existing = await _dismissalRepository.GetByTripAndMovementAsync(tripId, transactionId, cardTransactionId);
            if (existing != null) return; // idempotente

            await _dismissalRepository.AddAsync(new TripSuggestionDismissal
            {
                TripId = tripId,
                TransactionId = transactionId,
                CardTransactionId = cardTransactionId,
                UserId = userId
            });
        }

        private async Task<Trip> GetOwnedTripAsync(int userId, int id)
        {
            var trip = await _tripRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Trip not found");
            if (trip.UserId != userId) throw new UnauthorizedDomainException();
            return trip;
        }

        private async Task<Transaction> GetOwnedTransactionAsync(int userId, int id)
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(id)
                ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();
            return transaction;
        }

        private async Task<CardTransaction> GetOwnedCardTransactionAsync(int userId, int id)
        {
            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();
            return cardTransaction;
        }

        private async Task DeleteDismissalIfExistsAsync(int tripId, TripMovementRefDTO movementRef)
        {
            var (transactionId, cardTransactionId) = movementRef.Type == MovementTypeAccount
                ? ((int?)movementRef.Id, (int?)null)
                : ((int?)null, (int?)movementRef.Id);

            var dismissal = await _dismissalRepository.GetByTripAndMovementAsync(tripId, transactionId, cardTransactionId);
            if (dismissal != null)
                await _dismissalRepository.DeleteAsync(dismissal.Id);
        }

        // Regla de exclusión: pagos de cuotas (FK o prefijo legacy) y clases del sistema no son asociables
        private static void EnsureTransactionIsAssociable(Transaction transaction)
        {
            if (transaction.MovementType != "E")
                throw new BusinessRuleException("Solo un egreso puede asociarse a un viaje");

            if (transaction.CardTransactionId != null)
                throw new BusinessRuleException("Los pagos de cuotas de tarjeta no se asocian a viajes: el consumo de tarjeta se asocia directo");

            if (transaction.Detail != null && transaction.Detail.StartsWith(TripMovementRules.LegacyCardPaymentDetailPrefix))
                throw new BusinessRuleException("Los pagos de cuotas de tarjeta no se asocian a viajes: el consumo de tarjeta se asocia directo");

            if (transaction.TransactionClass != null
                && TripMovementRules.ExcludedTransactionClasses.Contains(transaction.TransactionClass.Description))
                throw new BusinessRuleException("La clase del movimiento no es asociable a un viaje");
        }

        private static TripMovementDTO MapAccountMovement(Transaction transaction)
        {
            return new TripMovementDTO
            {
                Id = transaction.Id,
                Origin = MovementTypeAccount,
                Date = transaction.Date,
                TransactionClass = transaction.TransactionClass?.Description,
                Detail = transaction.Detail,
                Amount = Math.Abs(transaction.Amount),
                Asset = transaction.Asset.Name,
                AssetSymbol = transaction.Asset.Symbol
            };
        }

        private static TripMovementDTO MapCardMovement(CardTransaction cardTransaction)
        {
            return new TripMovementDTO
            {
                Id = cardTransaction.Id,
                Origin = MovementTypeCard,
                Date = cardTransaction.Date,
                TransactionClass = cardTransaction.TransactionClass?.Description,
                Detail = cardTransaction.Detail,
                Amount = cardTransaction.TotalAmount,
                Asset = cardTransaction.Asset.Name,
                AssetSymbol = cardTransaction.Asset.Symbol
            };
        }

        private static void ValidateFields(string type, DateTime startDate, DateTime endDate)
        {
            if (!ValidTypes.Contains(type))
                throw new BusinessRuleException("Tipo de viaje inválido");

            if (endDate.Date < startDate.Date)
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la fecha de inicio");
        }

        private static TripDTO MapToDTO(Trip trip)
        {
            return new TripDTO
            {
                Id = trip.Id,
                Name = trip.Name,
                Type = trip.Type,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Status = GetStatus(trip)
            };
        }

        private static string GetStatus(Trip trip)
        {
            var today = DateTime.UtcNow.Date;
            if (today < trip.StartDate.Date) return "PLANNED";
            if (today > trip.EndDate.Date) return "FINISHED";
            return "IN_PROGRESS";
        }
    }
}
