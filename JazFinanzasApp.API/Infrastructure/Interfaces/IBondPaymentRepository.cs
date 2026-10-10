using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface IBondPaymentRepository
    {
        Task<IEnumerable<BondPayment>> GetByAssetIdAsync(int assetId);
        Task<BondPayment> GetByIdAsync(int id);

        // Pagos ya ocurridos / futuros de cualquier bono del catálogo, con Asset y CurrencyAsset
        // cargados (plan-amortizaciones-bonos.md, T9/T14).
        // Cronograma completo (pasados y futuros) de todos los bonos del catálogo — son unas decenas de
        // filas. Lo usa la proyección de próximos cobros, que necesita lo ya amortizado para el capital vivo.
        Task<IEnumerable<BondPayment>> GetAllAsync();
        Task<IEnumerable<BondPayment>> GetPastAsync(DateTime asOf);
        Task<IEnumerable<BondPayment>> GetFutureAsync(DateTime asOf);
    }
}
