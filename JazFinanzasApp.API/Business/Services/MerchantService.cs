using JazFinanzasApp.API.Business.DTO.Merchant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardTransactionRepository _cardTransactionRepository;
        private readonly IMerchantResolver _merchantResolver;

        public MerchantService(
            IMerchantRepository merchantRepository,
            ITransactionRepository transactionRepository,
            ICardTransactionRepository cardTransactionRepository,
            IMerchantResolver merchantResolver)
        {
            _merchantRepository = merchantRepository;
            _transactionRepository = transactionRepository;
            _cardTransactionRepository = cardTransactionRepository;
            _merchantResolver = merchantResolver;
        }

        public async Task<IEnumerable<MerchantListItemDTO>> GetAllForUserAsync(int userId)
        {
            var merchants = await _merchantRepository.GetByUserIdAsync(userId);
            var volumes = await _merchantRepository.GetVolumesByMerchantAsync(userId);

            return merchants
                .Select(m => new MerchantListItemDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    IsConfirmed = m.IsConfirmed,
                    Volume = volumes.GetValueOrDefault(m.Id)
                })
                .OrderByDescending(m => m.Volume)
                .ToList();
        }

        public async Task RenameMerchantAsync(int userId, int id, MerchantRenameDTO dto)
        {
            var merchant = await _merchantRepository.GetByIdAsync(id) ?? throw new NotFoundException("Merchant not found");
            if (merchant.UserId != userId) throw new UnauthorizedDomainException();

            // Renombrar es un acto de confirmación explícita — deja de ser "lo que agrupó el
            // resolver a ciegas" (Domain/Merchant.cs).
            merchant.Name = dto.Name;
            merchant.IsConfirmed = true;
            merchant.UpdatedAt = DateTime.UtcNow;
            await _merchantRepository.UpdateAsync(merchant);
        }

        public async Task MergeMerchantsAsync(int userId, int sourceMerchantId, int targetMerchantId)
        {
            if (sourceMerchantId == targetMerchantId)
                throw new BusinessRuleException("No se puede fusionar un comercio consigo mismo");

            var source = await _merchantRepository.GetByIdAsync(sourceMerchantId) ?? throw new NotFoundException("Merchant not found");
            if (source.UserId != userId) throw new UnauthorizedDomainException();

            var target = await _merchantRepository.GetByIdAsync(targetMerchantId) ?? throw new NotFoundException("Merchant not found");
            if (target.UserId != userId) throw new UnauthorizedDomainException();

            await _merchantRepository.MergeAsync(sourceMerchantId, targetMerchantId);
        }

        public async Task ReassignTransactionAsync(int userId, int transactionId, int merchantId)
        {
            var merchant = await _merchantRepository.GetByIdAsync(merchantId) ?? throw new NotFoundException("Merchant not found");
            if (merchant.UserId != userId) throw new UnauthorizedDomainException();

            var transaction = await _transactionRepository.GetByIdAsync(transactionId) ?? throw new NotFoundException("Transaction not found");
            if (transaction.UserId != userId) throw new UnauthorizedDomainException();

            transaction.MerchantId = merchantId;
            await _transactionRepository.UpdateAsync(transaction);

            await PropagateManualCorrectionAsync(merchantId, transaction.Detail);
        }

        public async Task ReassignCardTransactionAsync(int userId, int cardTransactionId, int merchantId)
        {
            var merchant = await _merchantRepository.GetByIdAsync(merchantId) ?? throw new NotFoundException("Merchant not found");
            if (merchant.UserId != userId) throw new UnauthorizedDomainException();

            var cardTransaction = await _cardTransactionRepository.GetByIdAsync(cardTransactionId) ?? throw new NotFoundException("Card transaction not found");
            if (cardTransaction.UserId != userId) throw new UnauthorizedDomainException();

            cardTransaction.MerchantId = merchantId;
            await _cardTransactionRepository.UpdateAsync(cardTransaction);

            await PropagateManualCorrectionAsync(merchantId, cardTransaction.Detail);
        }

        // Alcance de esta fase: procesa todo en un solo request (el checkpoint de la Fase 8b
        // pide un conjunto chico de prueba). Sobre el volumen real de años de movimientos, la
        // Fase 10 corre esto como backfill — ahí es donde vale la pena revisar performance
        // (batching de SaveChanges) si hiciera falta, no acá.
        public async Task<MerchantResolveBulkResultDTO> ResolveAllAsync(int userId)
        {
            var merchantsBefore = (await _merchantRepository.GetByUserIdAsync(userId)).Select(m => m.Id).ToHashSet();

            var unresolvedTransactions = await _merchantRepository.GetUnresolvedTransactionsAsync(userId);
            var transactionsResolved = 0;
            foreach (var transaction in unresolvedTransactions)
            {
                var merchantId = await _merchantResolver.ResolveAsync(userId, transaction.Detail);
                if (merchantId == null) continue;
                await _merchantRepository.SetTransactionMerchantAsync(transaction.Id, merchantId);
                transactionsResolved++;
            }

            var unresolvedCardTransactions = await _merchantRepository.GetUnresolvedCardTransactionsAsync(userId);
            var cardTransactionsResolved = 0;
            foreach (var cardTransaction in unresolvedCardTransactions)
            {
                var merchantId = await _merchantResolver.ResolveAsync(userId, cardTransaction.Detail);
                if (merchantId == null) continue;
                await _merchantRepository.SetCardTransactionMerchantAsync(cardTransaction.Id, merchantId);
                cardTransactionsResolved++;
            }

            var merchantsAfter = await _merchantRepository.GetByUserIdAsync(userId);
            var merchantsCreated = merchantsAfter.Count(m => !merchantsBefore.Contains(m.Id));

            return new MerchantResolveBulkResultDTO
            {
                TransactionsResolved = transactionsResolved,
                CardTransactionsResolved = cardTransactionsResolved,
                MerchantsCreated = merchantsCreated
            };
        }

        // T7: la corrección se propaga al texto normalizado (alias manual), no solo al movimiento
        // puntual que se está reasignando — así el resolver acierta solo la próxima vez.
        private async Task PropagateManualCorrectionAsync(int merchantId, string? detail)
        {
            var normalized = MerchantTextNormalizer.Normalize(detail);
            if (normalized.Length == 0) return;
            await _merchantRepository.UpsertManualAliasAsync(merchantId, normalized);
        }
    }
}
