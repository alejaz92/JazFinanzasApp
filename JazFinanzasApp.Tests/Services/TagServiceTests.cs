using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Tag;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class TagServiceTests
    {
        private readonly Mock<ITagRepository> _tagRepoMock;
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock;
        private readonly TagService _sut;

        private const int UserId = 1;
        private const int OtherUserId = 2;

        public TagServiceTests()
        {
            _tagRepoMock = new Mock<ITagRepository>();
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _cardTransactionRepoMock = new Mock<ICardTransactionRepository>();
            _sut = new TagService(_tagRepoMock.Object, _transactionRepoMock.Object, _cardTransactionRepoMock.Object);
        }

        // ── Alta ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateTagAsync_WithNewName_CreatesIt()
        {
            _tagRepoMock.Setup(r => r.GetByNameAsync("Auto", UserId)).ReturnsAsync((Tag)null!);
            var dto = new TagDTO { Name = "Auto", Color = "#ff0000" };

            await _sut.CreateTagAsync(UserId, dto);

            _tagRepoMock.Verify(r => r.AddAsync(It.Is<Tag>(t =>
                t.Name == "Auto" && t.Color == "#ff0000" && t.UserId == UserId)), Times.Once);
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateNameForSameUser_ThrowsBusinessRuleException()
        {
            _tagRepoMock.Setup(r => r.GetByNameAsync("Auto", UserId))
                .ReturnsAsync(new Tag { Id = 1, Name = "Auto", UserId = UserId });
            var dto = new TagDTO { Name = "Auto" };

            var act = () => _sut.CreateTagAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _tagRepoMock.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Never);
        }

        [Fact]
        public async Task CreateTagAsync_WithSameNameAsAnotherUsersTag_Succeeds()
        {
            // El nombre repetido solo importa dentro del mismo usuario.
            _tagRepoMock.Setup(r => r.GetByNameAsync("Auto", UserId)).ReturnsAsync((Tag)null!);
            var dto = new TagDTO { Name = "Auto" };

            await _sut.CreateTagAsync(UserId, dto);

            _tagRepoMock.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Once);
        }

        // ── Edición ──────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTagAsync_BelongingToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = OtherUserId, Name = "Auto" });
            var dto = new TagDTO { Name = "Casa" };

            var act = () => _sut.UpdateTagAsync(UserId, 5, dto);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _tagRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Tag>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTagAsync_RenamingToAnExistingOwnTagName_ThrowsBusinessRuleException()
        {
            var tag = new Tag { Id = 5, UserId = UserId, Name = "Auto" };
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(tag);
            _tagRepoMock.Setup(r => r.GetByNameAsync("Casa", UserId)).ReturnsAsync(new Tag { Id = 9, UserId = UserId, Name = "Casa" });
            var dto = new TagDTO { Name = "Casa" };

            var act = () => _sut.UpdateTagAsync(UserId, 5, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        // ── Borrado ──────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTagAsync_NotOwned_ThrowsUnauthorizedDomainException()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = OtherUserId });

            var act = () => _sut.DeleteTagAsync(UserId, 5);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _tagRepoMock.Verify(r => r.DeleteAsync(5), Times.Never);
        }

        [Fact]
        public async Task DeleteTagAsync_Owned_DeletesIt()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });

            await _sut.DeleteTagAsync(UserId, 5);

            _tagRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        // ── Asignación a un movimiento ───────────────────────────────────────

        [Fact]
        public async Task AssignToTransactionAsync_OwnedTagAndTransaction_AssignsIt()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(new Transaction { Id = 100, UserId = UserId });

            await _sut.AssignToTransactionAsync(UserId, 5, 100);

            _tagRepoMock.Verify(r => r.AssignToTransactionAsync(5, 100), Times.Once);
        }

        [Fact]
        public async Task AssignToTransactionAsync_TransactionBelongingToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(new Transaction { Id = 100, UserId = OtherUserId });

            var act = () => _sut.AssignToTransactionAsync(UserId, 5, 100);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _tagRepoMock.Verify(r => r.AssignToTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AssignToTransactionAsync_TagBelongingToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = OtherUserId });

            var act = () => _sut.AssignToTransactionAsync(UserId, 5, 100);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _tagRepoMock.Verify(r => r.AssignToTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // ── Desasignación de un movimiento ───────────────────────────────────

        [Fact]
        public async Task UnassignFromTransactionAsync_OwnedTagAndTransaction_UnassignsIt()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _transactionRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(new Transaction { Id = 100, UserId = UserId });

            await _sut.UnassignFromTransactionAsync(UserId, 5, 100);

            _tagRepoMock.Verify(r => r.UnassignFromTransactionAsync(5, 100), Times.Once);
        }

        // ── Asignación y desasignación a un consumo de tarjeta ───────────────

        [Fact]
        public async Task AssignToCardTransactionAsync_OwnedTagAndCardTransaction_AssignsIt()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(new CardTransaction { Id = 200, UserId = UserId });

            await _sut.AssignToCardTransactionAsync(UserId, 5, 200);

            _tagRepoMock.Verify(r => r.AssignToCardTransactionAsync(5, 200), Times.Once);
        }

        [Fact]
        public async Task AssignToCardTransactionAsync_CardTransactionBelongingToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(new CardTransaction { Id = 200, UserId = OtherUserId });

            var act = () => _sut.AssignToCardTransactionAsync(UserId, 5, 200);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _tagRepoMock.Verify(r => r.AssignToCardTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UnassignFromCardTransactionAsync_OwnedTagAndCardTransaction_UnassignsIt()
        {
            _tagRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { Id = 5, UserId = UserId });
            _cardTransactionRepoMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(new CardTransaction { Id = 200, UserId = UserId });

            await _sut.UnassignFromCardTransactionAsync(UserId, 5, 200);

            _tagRepoMock.Verify(r => r.UnassignFromCardTransactionAsync(5, 200), Times.Once);
        }
    }
}
