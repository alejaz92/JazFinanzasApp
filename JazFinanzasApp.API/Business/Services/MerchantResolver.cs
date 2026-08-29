using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    // Deducción de comercios (T7, plan-rediseno-reportes.md): normaliza el detalle y busca un
    // alias existente antes de crear un comercio nuevo — nunca pisa un alias ya creado, sea
    // manual o del propio resolver, así una re-ejecución nunca deshace una corrección del
    // usuario ni duplica comercios para el mismo texto.
    public class MerchantResolver : IMerchantResolver
    {
        private readonly IMerchantRepository _merchantRepository;

        public MerchantResolver(IMerchantRepository merchantRepository)
        {
            _merchantRepository = merchantRepository;
        }

        public async Task<int?> ResolveAsync(int userId, string? detail)
        {
            var normalized = MerchantTextNormalizer.Normalize(detail);
            if (normalized.Length == 0) return null;

            var existingAlias = await _merchantRepository.FindAliasAsync(userId, normalized);
            if (existingAlias != null) return existingAlias.MerchantId;

            var merchant = await _merchantRepository.CreateMerchantWithAliasAsync(userId, detail!.Trim(), normalized);
            return merchant.Id;
        }
    }
}
