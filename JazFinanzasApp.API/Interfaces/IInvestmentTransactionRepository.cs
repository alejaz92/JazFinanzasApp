using JazFinanzasApp.API.Models.Domain;

namespace JazFinanzasApp.API.Interfaces
{
    public interface IInvestmentTransactionRepository : IGenericRepository<InvestmentTransaction>
    {
        Task<InvestmentTransaction> GetInvestmentTransactionById(int id);
        Task<(IEnumerable<InvestmentTransaction> Transactions, int TotalCount)> GetPaginatedInvestmentTransactions(int userId, int page, int pageSize, string environment);
    }
}
