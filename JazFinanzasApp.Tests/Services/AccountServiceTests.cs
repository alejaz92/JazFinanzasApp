using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.Account;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    // Fase 5, docs/plans/activos/plan-rediseno-reportes.md: Account.Type + CountsAsLiquid.
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly Mock<IAssetTypeRepository> _assetTypeRepoMock;
        private readonly Mock<IAccount_AssetTypeRepository> _account_AssetTypeRepoMock;
        private readonly AccountService _sut;

        private const int UserId = 1;

        public AccountServiceTests()
        {
            _accountRepoMock = new Mock<IAccountRepository>();
            _assetTypeRepoMock = new Mock<IAssetTypeRepository>();
            _account_AssetTypeRepoMock = new Mock<IAccount_AssetTypeRepository>();
            _sut = new AccountService(_accountRepoMock.Object, _assetTypeRepoMock.Object, _account_AssetTypeRepoMock.Object);
        }

        // ── Type ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAccountAsync_WithInvalidType_ThrowsBusinessRuleException()
        {
            _accountRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
                .ReturnsAsync(new List<Account>());

            var dto = new AccountDTO { Name = "Efectivo", Type = "INVALID" };
            var act = () => _sut.CreateAccountAsync(UserId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>();
            _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateAccountAsync_WithValidType_Succeeds()
        {
            _accountRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
                .ReturnsAsync(new List<Account>());

            var dto = new AccountDTO { Name = "Banco Galicia", Type = AccountType.Bank };
            await _sut.CreateAccountAsync(UserId, dto);

            _accountRepoMock.Verify(r => r.AddAsync(It.Is<Account>(a => a.Type == AccountType.Bank)), Times.Once);
        }

        // ── CountsAsLiquid: default y preservación ──────────────────────────

        [Fact]
        public async Task CreateAccountAsync_WithoutCountsAsLiquid_DefaultsToTrue()
        {
            _accountRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
                .ReturnsAsync(new List<Account>());

            var dto = new AccountDTO { Name = "Broker de Bolsa", CountsAsLiquid = null };
            await _sut.CreateAccountAsync(UserId, dto);

            _accountRepoMock.Verify(r => r.AddAsync(It.Is<Account>(a => a.CountsAsLiquid == true)), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_WithCountsAsLiquidFalse_RespectsExplicitValue()
        {
            _accountRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
                .ReturnsAsync(new List<Account>());

            var dto = new AccountDTO { Name = "Broker de Bolsa", Type = AccountType.Investment, CountsAsLiquid = false };
            await _sut.CreateAccountAsync(UserId, dto);

            _accountRepoMock.Verify(r => r.AddAsync(It.Is<Account>(a => a.CountsAsLiquid == false)), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountAsync_WithoutCountsAsLiquid_DoesNotChangeExistingValue()
        {
            var account = new Account { Id = 1, UserId = UserId, Name = "Broker", CountsAsLiquid = false };
            _accountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

            var dto = new AccountDTO { Name = "Broker de Bolsa", CountsAsLiquid = null };
            await _sut.UpdateAccountAsync(UserId, 1, dto);

            account.CountsAsLiquid.Should().BeFalse();
            _accountRepoMock.Verify(r => r.UpdateAsync(It.Is<Account>(a => a.CountsAsLiquid == false)), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountAsync_WithExplicitCountsAsLiquid_OverridesExistingValue()
        {
            var account = new Account { Id = 1, UserId = UserId, Name = "Broker", CountsAsLiquid = true };
            _accountRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

            var dto = new AccountDTO { Name = "Broker de Bolsa", CountsAsLiquid = false };
            await _sut.UpdateAccountAsync(UserId, 1, dto);

            account.CountsAsLiquid.Should().BeFalse();
        }
    }
}
