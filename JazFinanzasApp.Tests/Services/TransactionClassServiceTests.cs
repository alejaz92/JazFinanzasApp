using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.TransactionClass;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class TransactionClassServiceTests
    {
        private readonly Mock<ITransactionClassRepository> _repoMock;
        private readonly TransactionClassService _sut;

        private const int UserId = 1;
        private const int OtherUserId = 2;

        public TransactionClassServiceTests()
        {
            _repoMock = new Mock<ITransactionClassRepository>();
            _sut = new TransactionClassService(_repoMock.Object);
        }

        private void NoDuplicateDescription()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransactionClass, bool>>>()))
                .ReturnsAsync(new List<TransactionClass>());
        }

        // ── Jerarquía: máximo dos niveles (T4) ───────────────────────────────

        [Fact]
        public async Task CreateTransactionClassAsync_WithParentThatHasNoParent_Succeeds()
        {
            NoDuplicateDescription();
            var rubro = new TransactionClass { Id = 10, UserId = UserId, Description = "Vivienda", ParentId = null };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(rubro);
            var dto = new TransactionClassDTO { Description = "Alquiler", IncExp = "E", ParentId = 10 };

            await _sut.CreateTransactionClassAsync(UserId, dto);

            _repoMock.Verify(r => r.AddAsync(It.Is<TransactionClass>(tc => tc.ParentId == 10)), Times.Once);
        }

        [Fact]
        public async Task CreateTransactionClassAsync_WithParentThatHasItsOwnParent_ThrowsBusinessRuleException()
        {
            NoDuplicateDescription();
            var subcategoria = new TransactionClass { Id = 20, UserId = UserId, Description = "Alquiler", ParentId = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(subcategoria);
            var dto = new TransactionClassDTO { Description = "Alquiler cochera", IncExp = "E", ParentId = 20 };

            var act = () => _sut.CreateTransactionClassAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.AddAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        [Fact]
        public async Task CreateTransactionClassAsync_WithParentBelongingToAnotherUser_ThrowsUnauthorizedDomainException()
        {
            NoDuplicateDescription();
            var ajenoRubro = new TransactionClass { Id = 10, UserId = OtherUserId, Description = "Vivienda", ParentId = null };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(ajenoRubro);
            var dto = new TransactionClassDTO { Description = "Alquiler", IncExp = "E", ParentId = 10 };

            var act = () => _sut.CreateTransactionClassAsync(UserId, dto);

            await act.Should().ThrowAsync<UnauthorizedDomainException>();
        }

        [Fact]
        public async Task CreateTransactionClassAsync_WithNonExistentParent_ThrowsBusinessRuleException()
        {
            NoDuplicateDescription();
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((TransactionClass)null!);
            var dto = new TransactionClassDTO { Description = "Alquiler", IncExp = "E", ParentId = 999 };

            var act = () => _sut.CreateTransactionClassAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task UpdateTransactionClassAsync_SetItselfAsOwnParent_ThrowsBusinessRuleException()
        {
            var tc = new TransactionClass { Id = 5, UserId = UserId, Description = "Ocio", ParentId = null };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(tc);
            var dto = new TransactionClassDTO { Description = "Ocio", IncExp = "E", ParentId = 5 };

            var act = () => _sut.UpdateTransactionClassAsync(UserId, 5, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTransactionClassAsync_MovingARubroWithChildrenUnderAnotherParent_ThrowsBusinessRuleException()
        {
            // "Vivienda" (id 10) ya tiene hijas (Alquiler cuelga de ella) — no puede pasar a ser subcategoría de otro rubro.
            var vivienda = new TransactionClass { Id = 10, UserId = UserId, Description = "Vivienda", ParentId = null };
            var otroRubro = new TransactionClass { Id = 30, UserId = UserId, Description = "Gastos Fijos", ParentId = null };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(vivienda);
            _repoMock.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(otroRubro);
            _repoMock.Setup(r => r.HasChildrenAsync(10)).ReturnsAsync(true);
            var dto = new TransactionClassDTO { Description = "Vivienda", IncExp = "E", ParentId = 30 };

            var act = () => _sut.UpdateTransactionClassAsync(UserId, 10, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTransactionClassAsync_RemovingParent_Succeeds()
        {
            var tc = new TransactionClass { Id = 20, UserId = UserId, Description = "Alquiler", ParentId = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(tc);
            var dto = new TransactionClassDTO { Description = "Alquiler", IncExp = "E", ParentId = null };

            await _sut.UpdateTransactionClassAsync(UserId, 20, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.Is<TransactionClass>(x => x.ParentId == null)), Times.Once);
        }

        // ── CountsAsIncomeExpense (T3) ────────────────────────────────────────

        [Fact]
        public async Task CreateTransactionClassAsync_DefaultsCountsAsIncomeExpenseToTrue()
        {
            NoDuplicateDescription();
            var dto = new TransactionClassDTO { Description = "Sueldo", IncExp = "I" };

            await _sut.CreateTransactionClassAsync(UserId, dto);

            _repoMock.Verify(r => r.AddAsync(It.Is<TransactionClass>(tc => tc.CountsAsIncomeExpense == true)), Times.Once);
        }

        [Fact]
        public async Task CreateTransactionClassAsync_WithCountsAsIncomeExpenseFalse_PersistsIt()
        {
            NoDuplicateDescription();
            var dto = new TransactionClassDTO { Description = "Ajuste Saldos Ingreso", IncExp = "I", CountsAsIncomeExpense = false };

            await _sut.CreateTransactionClassAsync(UserId, dto);

            _repoMock.Verify(r => r.AddAsync(It.Is<TransactionClass>(tc => tc.CountsAsIncomeExpense == false)), Times.Once);
        }

        // ── Borrado: no se puede borrar una categoría con subcategorías ──────

        [Fact]
        public async Task DeleteTransactionClassAsync_WithChildren_ThrowsBusinessRuleException()
        {
            var rubro = new TransactionClass { Id = 10, UserId = UserId, Description = "Vivienda" };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(rubro);
            _repoMock.Setup(r => r.IsTransactionClassInUseAsync(10)).ReturnsAsync(false);
            _repoMock.Setup(r => r.HasChildrenAsync(10)).ReturnsAsync(true);

            var act = () => _sut.DeleteTransactionClassAsync(UserId, 10);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _repoMock.Verify(r => r.DeleteAsync(10), Times.Never);
        }

        [Fact]
        public async Task DeleteTransactionClassAsync_WithoutChildrenNorUsage_DeletesIt()
        {
            var tc = new TransactionClass { Id = 20, UserId = UserId, Description = "Ocio" };
            _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(tc);
            _repoMock.Setup(r => r.IsTransactionClassInUseAsync(20)).ReturnsAsync(false);
            _repoMock.Setup(r => r.HasChildrenAsync(20)).ReturnsAsync(false);

            await _sut.DeleteTransactionClassAsync(UserId, 20);

            _repoMock.Verify(r => r.DeleteAsync(20), Times.Once);
        }
    }
}
