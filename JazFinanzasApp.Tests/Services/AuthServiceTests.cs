using FluentAssertions;
using JazFinanzasApp.API.Business.DTO.User;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace JazFinanzasApp.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IPortfolioRepository> _portfolioRepoMock;
        private readonly Mock<ITransactionClassRepository> _transactionClassRepoMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _portfolioRepoMock = new Mock<IPortfolioRepository>();
            _transactionClassRepoMock = new Mock<ITransactionClassRepository>();

            _sut = new AuthService(
                _userRepoMock.Object,
                _portfolioRepoMock.Object,
                _transactionClassRepoMock.Object);
        }

        // ── RegisterAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_WhenSucceeded_CreatesPortfolioAndTransactionClasses()
        {
            // Arrange
            var dto = new RegisterUserDTO
            {
                Name = "Juan",
                LastName = "Perez",
                UserName = "jperez",
                Email = "j@test.com",
                Password = "Pass123!"
            };

            _userRepoMock
                .Setup(r => r.RegisterUserAsync(dto.Name, dto.LastName, dto.UserName, dto.Email, dto.Password))
                .ReturnsAsync((IdentityResult.Success, 42));

            _portfolioRepoMock.Setup(r => r.AddAsync(It.IsAny<Portfolio>())).Returns(Task.CompletedTask);
            _transactionClassRepoMock.Setup(r => r.AddAsync(It.IsAny<TransactionClass>())).Returns(Task.CompletedTask);

            // Act
            var (succeeded, errors) = await _sut.RegisterAsync(dto);

            // Assert
            succeeded.Should().BeTrue();
            errors.Should().BeEmpty();

            _portfolioRepoMock.Verify(r => r.AddAsync(It.Is<Portfolio>(p =>
                p.UserId == 42 && p.IsDefault && p.Name == "Default")), Times.Once);

            _transactionClassRepoMock.Verify(r => r.AddAsync(It.IsAny<TransactionClass>()), Times.Exactly(5));
        }

        [Fact]
        public async Task RegisterAsync_WhenUsernameDuplicated_ReturnsFailed()
        {
            // Arrange
            var dto = new RegisterUserDTO
            {
                Name = "Juan",
                LastName = "Perez",
                UserName = "jperez",
                Email = "j@test.com",
                Password = "Pass123!"
            };

            var identityErrors = new[] { new IdentityError { Description = "Username already taken" } };
            var identityResult = IdentityResult.Failed(identityErrors);

            _userRepoMock
                .Setup(r => r.RegisterUserAsync(dto.Name, dto.LastName, dto.UserName, dto.Email, dto.Password))
                .ReturnsAsync((identityResult, 0));

            // Act
            var (succeeded, errors) = await _sut.RegisterAsync(dto);

            // Assert
            succeeded.Should().BeFalse();
            errors.Should().Contain("Username already taken");

            _portfolioRepoMock.Verify(r => r.AddAsync(It.IsAny<Portfolio>()), Times.Never);
            _transactionClassRepoMock.Verify(r => r.AddAsync(It.IsAny<TransactionClass>()), Times.Never);
        }

        // ── LoginAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var dto = new LoginUserDTO { UserName = "jperez", Password = "Pass123!" };
            const string expectedToken = "jwt.token.value";

            _userRepoMock
                .Setup(r => r.LoginUserAsync(dto.UserName, dto.Password))
                .ReturnsAsync(expectedToken);

            // Act
            var (succeeded, token, message) = await _sut.LoginAsync(dto);

            // Assert
            succeeded.Should().BeTrue();
            token.Should().Be(expectedToken);
            message.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
        {
            // Arrange
            var dto = new LoginUserDTO { UserName = "jperez", Password = "wrongpass" };

            _userRepoMock
                .Setup(r => r.LoginUserAsync(dto.UserName, dto.Password))
                .ReturnsAsync((string?)null);

            // Act
            var (succeeded, token, message) = await _sut.LoginAsync(dto);

            // Assert
            succeeded.Should().BeFalse();
            token.Should().BeNull();
            message.Should().Be("Invalid credentials");
        }
    }
}
