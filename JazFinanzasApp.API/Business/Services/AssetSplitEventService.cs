using JazFinanzasApp.API.Business.DTO.AssetSplitEvent;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class AssetSplitEventService : IAssetSplitEventService
    {
        private readonly IAssetSplitEventRepository _splitEventRepository;
        private readonly IAssetRepository _assetRepository;

        public AssetSplitEventService(
            IAssetSplitEventRepository splitEventRepository,
            IAssetRepository assetRepository)
        {
            _splitEventRepository = splitEventRepository;
            _assetRepository = assetRepository;
        }

        public async Task<IEnumerable<AssetSplitEventListDTO>> GetByAssetIdAsync(int assetId)
        {
            var events = await _splitEventRepository.GetByAssetIdAsync(assetId);
            return events.Select(e => new AssetSplitEventListDTO
            {
                Id = e.Id,
                AssetId = e.AssetId,
                AssetName = e.Asset.Name,
                Symbol = e.Asset.Symbol,
                Date = e.Date,
                SplitRatio = e.SplitRatio
            });
        }

        public async Task AddAsync(AssetSplitEventAddDTO dto, int userId)
        {
            if (dto.SplitRatio <= 0)
                throw new BusinessRuleException("SplitRatio must be greater than 0");

            if (dto.SplitRatio == 1)
                throw new BusinessRuleException("SplitRatio of 1 has no effect");

            if (dto.Date > DateTime.UtcNow)
                throw new BusinessRuleException("Split date cannot be in the future");

            var asset = await _assetRepository.GetAssetByIdAsync(dto.AssetId)
                ?? throw new NotFoundException("Asset not found");

            if (asset.AssetType.Environment != "BOLSA")
                throw new BusinessRuleException("Splits can only be registered for BOLSA assets");

            var existing = await _splitEventRepository.GetByAssetIdAsync(dto.AssetId);
            if (existing.Any(e => e.Date.Date == dto.Date.Date))
                throw new BusinessRuleException("A split for this asset on the same date already exists");

            await _splitEventRepository.AddAsync(new AssetSplitEvent
            {
                AssetId = dto.AssetId,
                Date = dto.Date,
                SplitRatio = dto.SplitRatio
            });
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var splitEvent = await _splitEventRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Split event not found");

            await _splitEventRepository.DeleteAsync(id);
        }
    }
}
