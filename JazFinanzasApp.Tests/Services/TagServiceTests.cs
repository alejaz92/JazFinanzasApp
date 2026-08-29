using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Tag;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 7, docs/plans/activos/plan-rediseno-reportes.md: crear, renombrar, asignar,
    // desasignar, no duplicar la misma etiqueta en un movimiento.
    public class TagServiceTests
    {
        private readonly Mock<ITagRepository> _repoMock;
        private readonly TagService _sut;

        private const int UserId = 1;

        public TagServiceTests()
        {
            _repoMock = new Mock<ITagRepository>();
            _sut = new TagService(_repoMock.Object);
        }

        // ── Crear ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateTagAsync_WithNewName_Succeeds()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>()))
                .ReturnsAsync(new List<Tag>());
            _repoMock.Setup(r => r.AddAsyncReturnObject(It.IsAny<Tag>()))
                .ReturnsAsync((Tag t) => { t.Id = 1; return t; });

            var dto = new TagAddDTO { Name = "Auto", Color = "#5B3DD9" };
            var result = await _sut.CreateTagAsync(UserId, dto);

            result.Name.Should().Be("Auto");
            _repoMock.Verify(r => r.AddAsyncReturnObject(It.Is<Tag>(t => t.Name == "Auto" && t.UserId == UserId)), Times.Once);
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateName_ThrowsBusinessRuleException()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>()))
                .ReturnsAsync(new List<Tag> { new Tag { Id = 1, Name = "Auto", UserId = UserId } });

            var dto = new TagAddDTO { Name = "Auto" };
            var act = () => _sut.CreateTagAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AddAsyncReturnObject(It.IsAny<Tag>()), Times.Never);
        }

        // ── Renombrar ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTagAsync_WithNewName_Succeeds()
        {
            var tag = new Tag { Id = 1, UserId = UserId, Name = "Auto" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tag);
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>()))
                .ReturnsAsync(new List<Tag>());

            var dto = new TagEditDTO { Name = "Vehículo" };
            await _sut.UpdateTagAsync(UserId, 1, dto);

            tag.Name.Should().Be("Vehículo");
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Tag>(t => t.Name == "Vehículo")), Times.Once);
        }

        [Fact]
        public async Task UpdateTagAsync_OfAnotherUser_ThrowsUnauthorizedDomainException()
        {
            var tag = new Tag { Id = 1, UserId = 999, Name = "Auto" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tag);

            var dto = new TagEditDTO { Name = "Vehículo" };
            var act = () => _sut.UpdateTagAsync(UserId, 1, dto);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        // ── Asignar ───────────────────────────────────────────────────────────

        [Fact]
        public async Task AssignToTransactionAsync_NotYetAssigned_Succeeds()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetTransactionOwnerIdAsync(10)).ReturnsAsync(UserId);
            _repoMock.Setup(r => r.IsAssignedToTransactionAsync(10, 1)).ReturnsAsync(false);

            await _sut.AssignToTransactionAsync(UserId, transactionId: 10, tagId: 1);

            _repoMock.Verify(r => r.AssignToTransactionAsync(10, 1), Times.Once);
        }

        [Fact]
        public async Task AssignToCardTransactionAsync_NotYetAssigned_Succeeds()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetCardTransactionOwnerIdAsync(20)).ReturnsAsync(UserId);
            _repoMock.Setup(r => r.IsAssignedToCardTransactionAsync(20, 1)).ReturnsAsync(false);

            await _sut.AssignToCardTransactionAsync(UserId, cardTransactionId: 20, tagId: 1);

            _repoMock.Verify(r => r.AssignToCardTransactionAsync(20, 1), Times.Once);
        }

        [Fact]
        public async Task AssignToTransactionAsync_OfAnotherUsersTransaction_ThrowsUnauthorizedDomainException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetTransactionOwnerIdAsync(10)).ReturnsAsync(999);

            var act = () => _sut.AssignToTransactionAsync(UserId, transactionId: 10, tagId: 1);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
            _repoMock.Verify(r => r.AssignToTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // ── No duplicar la misma etiqueta en un movimiento ───────────────────

        [Fact]
        public async Task AssignToTransactionAsync_AlreadyAssigned_ThrowsBusinessRuleException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetTransactionOwnerIdAsync(10)).ReturnsAsync(UserId);
            _repoMock.Setup(r => r.IsAssignedToTransactionAsync(10, 1)).ReturnsAsync(true);

            var act = () => _sut.AssignToTransactionAsync(UserId, transactionId: 10, tagId: 1);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AssignToTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AssignToCardTransactionAsync_AlreadyAssigned_ThrowsBusinessRuleException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetCardTransactionOwnerIdAsync(20)).ReturnsAsync(UserId);
            _repoMock.Setup(r => r.IsAssignedToCardTransactionAsync(20, 1)).ReturnsAsync(true);

            var act = () => _sut.AssignToCardTransactionAsync(UserId, cardTransactionId: 20, tagId: 1);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AssignToCardTransactionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // ── Desasignar ────────────────────────────────────────────────────────

        [Fact]
        public async Task UnassignFromTransactionAsync_Succeeds()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetTransactionOwnerIdAsync(10)).ReturnsAsync(UserId);

            await _sut.UnassignFromTransactionAsync(UserId, transactionId: 10, tagId: 1);

            _repoMock.Verify(r => r.UnassignFromTransactionAsync(10, 1), Times.Once);
        }

        [Fact]
        public async Task UnassignFromCardTransactionAsync_Succeeds()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });
            _repoMock.Setup(r => r.GetCardTransactionOwnerIdAsync(20)).ReturnsAsync(UserId);

            await _sut.UnassignFromCardTransactionAsync(UserId, cardTransactionId: 20, tagId: 1);

            _repoMock.Verify(r => r.UnassignFromCardTransactionAsync(20, 1), Times.Once);
        }

        // ── Borrar (limpia asignaciones, no las bloquea — sección 7 del plan) ─

        [Fact]
        public async Task DeleteTagAsync_DeletesTagWithAssignments()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tag { Id = 1, UserId = UserId, Name = "Auto" });

            await _sut.DeleteTagAsync(UserId, 1);

            _repoMock.Verify(r => r.DeleteTagWithAssignmentsAsync(1), Times.Once);
        }
    }
}
