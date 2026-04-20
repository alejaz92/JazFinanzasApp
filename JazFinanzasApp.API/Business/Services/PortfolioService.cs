using JazFinanzasApp.API.Business.DTO.Portfolio;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepository;

        public PortfolioService(IPortfolioRepository portfolioRepository)
        {
            _portfolioRepository = portfolioRepository;
        }

        public async Task<IEnumerable<PortfolioDTO>> GetAllForUserAsync(int userId)
        {
            var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
            return portfolios.Select(p => new PortfolioDTO { Id = p.Id, Name = p.Name, IsDefault = p.IsDefault });
        }

        public async Task<PortfolioDTO> GetByIdAsync(int userId, int id)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Portfolio not found");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();
            return new PortfolioDTO { Id = portfolio.Id, Name = portfolio.Name, IsDefault = portfolio.IsDefault };
        }

        public async Task CreatePortfolioAsync(int userId, PortfolioDTO dto)
        {
            var existing = await _portfolioRepository.FindAsync(p => p.Name == dto.Name && p.UserId == userId);
            if (existing.Any()) throw new BusinessRuleException("Portfolio with this name already exists.");

            if (dto.IsDefault)
            {
                var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId);
                if (defaultPortfolio != null) throw new BusinessRuleException("User already has a default portfolio.");
            }

            await _portfolioRepository.AddAsync(new Portfolio
            {
                Name = dto.Name, UserId = userId, IsDefault = dto.IsDefault
            });
        }

        public async Task UpdatePortfolioAsync(int userId, int id, PortfolioDTO dto)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Portfolio not found");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();

            var existing = await _portfolioRepository.FindAsync(p => p.Name == dto.Name && p.UserId == userId && p.Id != id);
            if (existing.Any()) throw new BusinessRuleException("Portfolio with this name already exists.");

            portfolio.Name = dto.Name;
            await _portfolioRepository.UpdateAsync(portfolio);
        }

        public async Task DeletePortfolioAsync(int userId, int id)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Portfolio not found");
            if (portfolio.UserId != userId) throw new UnauthorizedDomainException();
            if (portfolio.IsDefault) throw new BusinessRuleException("Default portfolio cannot be deleted.");
            if (await _portfolioRepository.IsPortfolioUsedInTransactions(id))
                throw new BusinessRuleException("Portfolio is used in transactions and cannot be deleted.");
            await _portfolioRepository.DeleteAsync(id);
        }

        public async Task<PortfolioDTO> GetDefaultPortfolioAsync(int userId)
        {
            var portfolio = await _portfolioRepository.GetDefaultPortfolio(userId)
                ?? throw new NotFoundException("Default portfolio not found");
            return new PortfolioDTO { Id = portfolio.Id, Name = portfolio.Name, IsDefault = portfolio.IsDefault };
        }
    }
}
