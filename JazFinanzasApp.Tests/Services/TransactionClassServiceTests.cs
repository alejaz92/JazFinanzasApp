using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.TransactionClass;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 5, docs/plans/activos/plan-rediseno-reportes.md (T13): jerarquía de un solo nivel
    // para TransactionClass, más la validación de Nature.
    public class TransactionClassServiceTests
    {
        private readonly Mock<ITransactionClassRepository> _repoMock;
        private readonly TransactionClassService _sut;

        private const int UserId = 1;

        public TransactionClassServiceTests()
        {
            _repoMock = new Mock<ITransactionClassRepository>();
            _sut = new TransactionClassService(_repoMock.Object);
        }

        // ── Jerarquía: un padre no puede tener padre ─────────────────────────

        [Fact]
        public async Task CreateTransactionClassAsync_WithParentThatHasItsOwnParent_ThrowsBusinessRuleException()
        {
            var grandparent = new TransactionClass { Id = 1, UserId = UserId, Description = "Comida" };
            var parent = new TransactionClass { Id = 2, UserId = UserId, Description = "Supermercado", ParentId = 1 };
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass>());
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(parent);

            var dto = new TransactionClassDTO { Description = "Verdulería", ParentId = 2 };
            var act = () => _sut.CreateTransactionClassAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AddAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTransactionClassAsync_AssigningParentThatHasItsOwnParent_ThrowsBusinessRuleException()
        {
            var tc = new TransactionClass { Id = 3, UserId = UserId, Description = "Farmacia" };
            var parent = new TransactionClass { Id = 2, UserId = UserId, Description = "Supermercado", ParentId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(tc);
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(parent);

            var dto = new TransactionClassDTO { Description = "Farmacia", ParentId = 2 };
            var act = () => _sut.UpdateTransactionClassAsync(UserId, 3, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        // ── Jerarquía: no auto-referencia ────────────────────────────────────

        [Fact]
        public async Task UpdateTransactionClassAsync_WithSelfAsParent_ThrowsBusinessRuleException()
        {
            var tc = new TransactionClass { Id = 5, UserId = UserId, Description = "Comida" };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(tc);

            var dto = new TransactionClassDTO { Description = "Comida", ParentId = 5 };
            var act = () => _sut.UpdateTransactionClassAsync(UserId, 5, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        // ── Jerarquía: una categoría con hijos no puede pasar a tener padre ──

        [Fact]
        public async Task UpdateTransactionClassAsync_WithChildren_CannotBecomeChildItself()
        {
            var tc = new TransactionClass { Id = 1, UserId = UserId, Description = "Comida" };
            var otherParent = new TransactionClass { Id = 9, UserId = UserId, Description = "Gastos Fijos" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tc);
            _repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(otherParent);
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass> { new TransactionClass { Id = 2, ParentId = 1 } }); // ya tiene un hijo

            var dto = new TransactionClassDTO { Description = "Comida", ParentId = 9 };
            var act = () => _sut.UpdateTransactionClassAsync(UserId, 1, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        // ── Jerarquía: caso feliz ─────────────────────────────────────────────

        [Fact]
        public async Task CreateTransactionClassAsync_WithValidParent_Succeeds()
        {
            var parent = new TransactionClass { Id = 1, UserId = UserId, Description = "Comida" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parent);
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass>());

            var dto = new TransactionClassDTO { Description = "Supermercado", ParentId = 1 };
            await _sut.CreateTransactionClassAsync(UserId, dto);

            _repoMock.Verify(r => r.AddAsync(It.Is<TransactionClass>(tc => tc.ParentId == 1 && tc.Description == "Supermercado")), Times.Once);
        }

        // ── Nature ────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateTransactionClassAsync_WithInvalidNature_ThrowsBusinessRuleException()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass>());

            var dto = new TransactionClassDTO { Description = "Comida", Nature = "INVALID" };
            var act = () => _sut.CreateTransactionClassAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AddAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        [Fact]
        public async Task CreateTransactionClassAsync_WithValidNature_Succeeds()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass>());

            var dto = new TransactionClassDTO { Description = "Comida", Nature = TransactionClassNature.Essential };
            await _sut.CreateTransactionClassAsync(UserId, dto);

            _repoMock.Verify(r => r.AddAsync(It.Is<TransactionClass>(tc => tc.Nature == TransactionClassNature.Essential)), Times.Once);
        }
    }
}
