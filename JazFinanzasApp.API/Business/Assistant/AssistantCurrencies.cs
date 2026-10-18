using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Las dos monedas en las que viaja todo el contexto (plan-devoluciones-llm.md, decisión 9).
    public record AssistantCurrencySet(Asset Ars, Asset Usd);

    public interface IAssistantCurrencyResolver
    {
        Task<AssistantCurrencySet> GetAsync();
    }

    // Mismo mecanismo que CardReportService para encontrar pesos y dólares: por nombre del activo.
    public class AssistantCurrencyResolver : IAssistantCurrencyResolver
    {
        public const string PesoAssetName = "Peso Argentino";
        public const string DollarAssetName = "Dolar Estadounidense";

        private readonly IAssetRepository _assetRepository;

        public AssistantCurrencyResolver(IAssetRepository assetRepository)
        {
            _assetRepository = assetRepository;
        }

        public async Task<AssistantCurrencySet> GetAsync()
        {
            var ars = await _assetRepository.GetAssetByNameAsync(PesoAssetName)
                ?? throw new InvalidOperationException($"No se encontró el activo '{PesoAssetName}'.");
            var usd = await _assetRepository.GetAssetByNameAsync(DollarAssetName)
                ?? throw new InvalidOperationException($"No se encontró el activo '{DollarAssetName}'.");
            return new AssistantCurrencySet(ars, usd);
        }
    }
}
