using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Card;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class CardServiceTests
    {
        private readonly Mock<ICardRepository> _cardRepoMock;
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepoMock;
        private readonly CardService _sut;

        private const int UserId = 1;

        public CardServiceTests()
        {
            _cardRepoMock = new Mock<ICardRepository>();
            _cardPaymentRepoMock = new Mock<ICardPaymentRepository>();
            _sut = new CardService(_cardRepoMock.Object, _cardPaymentRepoMock.Object);
        }

        // ── CreateCardAsync / UpdateCardAsync — validación D5 ────────────────

        [Fact]
        public async Task CreateCardAsync_WithNextDueDateBeforeNextClosingDate_ThrowsBusinessRuleException()
        {
            var dto = new CardDTO
            {
                Name = "Visa",
                NextClosingDate = new DateTime(2026, 9, 20),
                NextDueDate = new DateTime(2026, 9, 10)
            };
            _cardRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Card, bool>>>()))
                .ReturnsAsync(new List<Card>());

            var act = () => _sut.CreateCardAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _cardRepoMock.Verify(r => r.AddAsync(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCardAsync_WithNextDueDateBeforeNextClosingDate_ThrowsBusinessRuleException()
        {
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa" };
            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);

            var dto = new CardDTO
            {
                Name = "Visa",
                NextClosingDate = new DateTime(2026, 9, 20),
                NextDueDate = new DateTime(2026, 9, 10)
            };

            var act = () => _sut.UpdateCardAsync(UserId, 1, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _cardRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCardAsync_WithValidDates_UpdatesCard()
        {
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa" };
            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);

            var dto = new CardDTO
            {
                Name = "Visa",
                NextClosingDate = new DateTime(2026, 9, 20),
                NextDueDate = new DateTime(2026, 9, 27)
            };

            await _sut.UpdateCardAsync(UserId, 1, dto);

            _cardRepoMock.Verify(r => r.UpdateAsync(It.Is<Card>(c =>
                c.NextClosingDate == dto.NextClosingDate && c.NextDueDate == dto.NextDueDate)), Times.Once);
        }

        // ── IsCurrentPeriodPaid (D6) ──────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WithNoNextClosingDate_ReturnsIsCurrentPeriodPaidFalse()
        {
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa", NextClosingDate = null };
            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);

            var result = await _sut.GetByIdAsync(UserId, 1);

            result.IsCurrentPeriodPaid.Should().BeFalse();
            _cardPaymentRepoMock.Verify(r => r.IsPaymentAlreadyMadeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WithNextClosingDateAlreadyPaid_ReturnsIsCurrentPeriodPaidTrue()
        {
            var closingDate = new DateTime(2026, 9, 20);
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa", NextClosingDate = closingDate };
            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);
            _cardPaymentRepoMock.Setup(r => r.IsPaymentAlreadyMadeAsync(1, closingDate)).ReturnsAsync(true);

            var result = await _sut.GetByIdAsync(UserId, 1);

            result.IsCurrentPeriodPaid.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_WithNextClosingDateNotPaid_ReturnsIsCurrentPeriodPaidFalse()
        {
            var closingDate = new DateTime(2026, 9, 20);
            var card = new Card { Id = 1, UserId = UserId, Name = "Visa", NextClosingDate = closingDate };
            _cardRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(card);
            _cardPaymentRepoMock.Setup(r => r.IsPaymentAlreadyMadeAsync(1, closingDate)).ReturnsAsync(false);

            var result = await _sut.GetByIdAsync(UserId, 1);

            result.IsCurrentPeriodPaid.Should().BeFalse();
        }
    }
}
