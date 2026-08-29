using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Merchant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 8b, docs/plans/activos/plan-rediseno-reportes.md: resolver masivo sobre un conjunto
    // chico deja los MerchantId escritos; fusionar dos comercios reasigna movimientos y alias
    // sin perder ninguno.
    public class MerchantServiceTests
    {
        private readonly Mock<IMerchantRepository> _merchantRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly Mock<IMerchantResolver> _resolverMock;
        private readonly MerchantService _sut;

        private const int UserId = 1;

        public MerchantServiceTests()
        {
            _merchantRepoMock = new Mock<IMerchantRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _resolverMock = new Mock<IMerchantResolver>();
            _sut = new MerchantService(_merchantRepoMock.Object, _transactionRepoMock.Object, _cardTransactionRepoMock.Object, _resolverMock.Object);
        }

        // ── Resolver masivo ───────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAllAsync_OverSmallSet_WritesMerchantIdOnEveryResolvedRow()
        {
            var pendingTransactions = new List<Transaction>
            {
                new Transaction { Id = 1, UserId = UserId, Detail = "Compra Coto" },
                new Transaction { Id = 2, UserId = UserId, Detail = "COTO 3456" },
                new Transaction { Id = 3, UserId = UserId, Detail = "" } // no resuelve
            };
            var pendingCardTransactions = new List<CardTransaction>
            {
                new CardTransaction { Id = 10, UserId = UserId, Detail = "Farmacia del Sol" }
            };

            _merchantRepoMock.SetupSequence(r => r.GetByUserIdAsync(UserId))
                .ReturnsAsync(new List<Merchant>()) // antes de resolver: ninguno
                .ReturnsAsync(new List<Merchant> // después: dos comercios nuevos
                {
                    new Merchant { Id = 100, UserId = UserId, Name = "Coto" },
                    new Merchant { Id = 200, UserId = UserId, Name = "Farmacia del Sol" }
                });
            _merchantRepoMock.Setup(r => r.GetUnresolvedTransactionsAsync(UserId)).ReturnsAsync(pendingTransactions);
            _merchantRepoMock.Setup(r => r.GetUnresolvedCardTransactionsAsync(UserId)).ReturnsAsync(pendingCardTransactions);

            _resolverMock.Setup(r => r.ResolveAsync(UserId, "Compra Coto")).ReturnsAsync(100);
            _resolverMock.Setup(r => r.ResolveAsync(UserId, "COTO 3456")).ReturnsAsync(100);
            _resolverMock.Setup(r => r.ResolveAsync(UserId, "")).ReturnsAsync((int?)null); // detalle vacío
            _resolverMock.Setup(r => r.ResolveAsync(UserId, "Farmacia del Sol")).ReturnsAsync(200);

            var result = await _sut.ResolveAllAsync(UserId);

            result.TransactionsResolved.Should().Be(2);
            result.CardTransactionsResolved.Should().Be(1);
            result.MerchantsCreated.Should().Be(2);

            _merchantRepoMock.Verify(r => r.SetTransactionMerchantAsync(1, 100), Times.Once);
            _merchantRepoMock.Verify(r => r.SetTransactionMerchantAsync(2, 100), Times.Once);
            _merchantRepoMock.Verify(r => r.SetTransactionMerchantAsync(3, It.IsAny<int?>()), Times.Never);
            _merchantRepoMock.Verify(r => r.SetCardTransactionMerchantAsync(10, 200), Times.Once);
        }

        [Fact]
        public async Task ResolveAllAsync_WithNothingPending_ResolvesNothing()
        {
            _merchantRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(new List<Merchant>());
            _merchantRepoMock.Setup(r => r.GetUnresolvedTransactionsAsync(UserId)).ReturnsAsync(new List<Transaction>());
            _merchantRepoMock.Setup(r => r.GetUnresolvedCardTransactionsAsync(UserId)).ReturnsAsync(new List<CardTransaction>());

            var result = await _sut.ResolveAllAsync(UserId);

            result.TransactionsResolved.Should().Be(0);
            result.CardTransactionsResolved.Should().Be(0);
            result.MerchantsCreated.Should().Be(0);
        }

        // ── Fusionar ──────────────────────────────────────────────────────────

        [Fact]
        public async Task MergeMerchantsAsync_CallsRepositoryMergeWithBothIds()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, UserId = UserId, Name = "Coto SA" });
            _merchantRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Merchant { Id = 2, UserId = UserId, Name = "Coto" });

            await _sut.MergeMerchantsAsync(UserId, sourceMerchantId: 1, targetMerchantId: 2);

            _merchantRepoMock.Verify(r => r.MergeAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task MergeMerchantsAsync_SameMerchantTwice_ThrowsBusinessRuleException()
        {
            var act = () => _sut.MergeMerchantsAsync(UserId, sourceMerchantId: 1, targetMerchantId: 1);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _merchantRepoMock.Verify(r => r.MergeAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MergeMerchantsAsync_OfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, UserId = 999, Name = "Coto SA" });

            var act = () => _sut.MergeMerchantsAsync(UserId, sourceMerchantId: 1, targetMerchantId: 2);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _merchantRepoMock.Verify(r => r.MergeAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // ── Reasignar (propaga la corrección como alias manual) ─────────────

        [Fact]
        public async Task ReassignTransactionAsync_UpdatesTransactionAndUpsertsManualAlias()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Merchant { Id = 2, UserId = UserId, Name = "Farmacia del Sol" });
            var transaction = new Transaction { Id = 5, UserId = UserId, Detail = "Compra Farmacia" };
            _transactionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(transaction);

            await _sut.ReassignTransactionAsync(UserId, transactionId: 5, merchantId: 2);

            transaction.MerchantId.Should().Be(2);
            _transactionRepoMock.Verify(r => r.UpdateAsync(transaction), Times.Once);
            _merchantRepoMock.Verify(r => r.UpsertManualAliasAsync(2, "farmacia"), Times.Once);
        }

        [Fact]
        public async Task ReassignTransactionAsync_OfAnotherUsersTransaction_ThrowsUnauthorizedDomainException()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Merchant { Id = 2, UserId = UserId, Name = "Farmacia" });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Transaction { Id = 5, UserId = 999, Detail = "x" });

            var act = () => _sut.ReassignTransactionAsync(UserId, transactionId: 5, merchantId: 2);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── Ver movimientos (Fase 9: endpoint que ningún fase anterior cubrió) ─

        [Fact]
        public async Task GetMovementsAsync_MergesTransactionsAndCardTransactionsOrderedByDateDescending()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, UserId = UserId, Name = "Coto" });
            _merchantRepoMock.Setup(r => r.GetTransactionsByMerchantAsync(1)).ReturnsAsync(new List<Transaction>
            {
                new Transaction { Id = 5, Date = new DateTime(2026, 1, 10), Detail = "Compra Coto", Amount = -100 }
            });
            _merchantRepoMock.Setup(r => r.GetCardTransactionsByMerchantAsync(1)).ReturnsAsync(new List<CardTransaction>
            {
                new CardTransaction { Id = 9, Date = new DateTime(2026, 2, 1), Detail = "Coto 3456", TotalAmount = 200 }
            });

            var result = (await _sut.GetMovementsAsync(UserId, 1)).ToList();

            result.Should().HaveCount(2);
            result[0].Source.Should().Be("CardTransaction"); // más reciente primero
            result[1].Source.Should().Be("Transaction");
        }

        [Fact]
        public async Task GetMovementsAsync_OfAnotherUsersMerchant_ThrowsUnauthorizedDomainException()
        {
            _merchantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Merchant { Id = 1, UserId = 999, Name = "Coto" });

            var act = () => _sut.GetMovementsAsync(UserId, 1);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── Renombrar ─────────────────────────────────────────────────────────

        [Fact]
        public async Task RenameMerchantAsync_UpdatesNameAndMarksConfirmed()
        {
            var merchant = new Merchant { Id = 1, UserId = UserId, Name = "coto", IsConfirmed = false };
            _merchantRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(merchant);

            await _sut.RenameMerchantAsync(UserId, 1, new MerchantRenameDTO { Name = "Coto Supermercados" });

            merchant.Name.Should().Be("Coto Supermercados");
            merchant.IsConfirmed.Should().BeTrue();
            _merchantRepoMock.Verify(r => r.UpdateAsync(merchant), Times.Once);
        }
    }
}
