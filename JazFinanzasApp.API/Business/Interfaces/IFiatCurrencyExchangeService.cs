using JazFinanzasApp.API.Business.DTO.InvestmentTransaction;

namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IFiatCurrencyExchangeService
    {
        Task<(IEnumerable<CurrencyExchangeListDTO> Transactions, int TotalCount)> GetPaginatedAsync(int userId, int page, int pageSize);
        Task CreateExchangeTransactionAsync(int userId, CurrencyExchangeAddDTO dto);
        Task DeleteExchangeTransactionAsync(int userId, int id);
    }
}
