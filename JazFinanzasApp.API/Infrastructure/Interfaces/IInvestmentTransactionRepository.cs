using JazFinanzasApp.API.Infrastructure.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IInvestmentTransactionRepository : IGenericRepository<InvestmentTransaction>
    {
        Task<InvestmentTransaction> GetInvestmentTransactionById(int id);
        Task<(IEnumerable<InvestmentTransaction> Transactions, int TotalCount)> GetPaginatedInvestmentTransactions(int userId, int page, int pageSize, string environment);
    }
}
