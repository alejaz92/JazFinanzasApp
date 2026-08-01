using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Mismo criterio que TransactionService.ResolveQuotePriceAsync cuando no viene una cotización
    // explícita del usuario: USD no cotiza contra sí mismo, ARS usa la BLUE del día y el resto la NA.
    // GetQuotePrice ya resuelve el día anterior más cercano cuando la fecha exacta no tiene cotización.
    public class QuotePriceResolver : IQuotePriceResolver
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetQuoteRepository _assetQuoteRepository;

        public QuotePriceResolver(IAssetRepository assetRepository, IAssetQuoteRepository assetQuoteRepository)
        {
            _assetRepository = assetRepository;
            _assetQuoteRepository = assetQuoteRepository;
        }

        public async Task<decimal> ResolveAsync(int assetId, DateTime date)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId)
                ?? throw new NotFoundException("Activo no encontrado");

            if (asset.Symbol == "USD") return 1;

            var type = asset.Symbol == "ARS" ? "BLUE" : "NA";
            return await _assetQuoteRepository.GetQuotePrice(assetId, date, type);
        }
    }
}
