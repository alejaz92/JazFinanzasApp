using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IBondPaymentRepository
    {
        Task<IEnumerable<BondPayment>> GetByAssetIdAsync(int assetId);
        Task<BondPayment> GetByIdAsync(int id);

        // Pagos ya ocurridos / futuros de cualquier bono del catálogo, con Asset y CurrencyAsset
        // cargados (plan-amortizaciones-bonos.md, T9/T14).
        Task<IEnumerable<BondPayment>> GetPastAsync(DateTime asOf);
        Task<IEnumerable<BondPayment>> GetFutureAsync(DateTime asOf);
    }
}
