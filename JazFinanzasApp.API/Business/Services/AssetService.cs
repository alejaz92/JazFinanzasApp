using JazFinanzasApp.API.Business.DTO.Asset;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using AssetTypeDTO = JazFinanzasApp.API.Business.DTO.AssetType.AssetTypeDTO;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAsset_UserRepository _asset_UserRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;

        public AssetService(
            IAssetRepository assetRepository,
            IAsset_UserRepository asset_UserRepository,
            IAssetTypeRepository assetTypeRepository)
        {
            _assetRepository = assetRepository;
            _asset_UserRepository = asset_UserRepository;
            _assetTypeRepository = assetTypeRepository;
        }

        public async Task<IEnumerable<AssetDTO>> GetAllAssetsAsync()
        {
            var assets = await _assetRepository.GetAssetsAsync();
            return assets.Select(a => new AssetDTO
            {
                Id = a.Id, Name = a.Name, Symbol = a.Symbol, AssetTypeName = a.AssetType.Name
            });
        }

        public async Task<IEnumerable<AssetTypeDTO>> GetAssetTypesAsync()
        {
            var types = await _assetTypeRepository.GetAllAsync();
            return types.Select(at => new AssetTypeDTO { Id = at.Id, Name = at.Name });
        }

        public async Task<IEnumerable<AssetTypeDTO>> GetAssetTypesByEnvironmentAsync(string environment)
        {
            var types = await _assetTypeRepository.GetAssetTypes(environment);
            return types.Select(at => new AssetTypeDTO { Id = at.Id, Name = at.Name });
        }

        public async Task<IEnumerable<AssetDTO>> GetAssetsByTypeAsync(int assetTypeId)
        {
            var assetType = await _assetTypeRepository.GetByIdAsync(assetTypeId)
                ?? throw new NotFoundException("Asset type not found");
            var assets = await _assetRepository.GetAssetsByTypeAsync(assetTypeId);
            return assets.Select(a => new AssetDTO
            {
                Id = a.Id, Name = a.Name, Symbol = a.Symbol, AssetTypeName = a.AssetType.Name
            });
        }

        public async Task<IEnumerable<AssetDTO>> GetUserAssetsAsync(int userId, int assetTypeId)
        {
            var assets = await _asset_UserRepository.GetUserAssetsAsync(userId, assetTypeId);
            return assets.Select(a => new AssetDTO
            {
                Id = a.AssetId, Name = a.Asset.Name, Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name,
                IsReference = a.isReference, IsMainReference = a.isMainReference
            });
        }

        public async Task<IEnumerable<AssetDTO>> GetUserAssetsByTypeNameAsync(int userId, string assetTypeName)
        {
            var assetType = await _assetTypeRepository.GetByName(assetTypeName)
                ?? throw new NotFoundException("Asset type not found");
            var assets = await _asset_UserRepository.GetUserAssetsAsync(userId, assetType.Id);
            return assets.Select(a => new AssetDTO
            {
                Id = a.AssetId, Name = a.Asset.Name, Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name
            });
        }

        public async Task<IEnumerable<AssetDTO>> GetAssetsForCardTransactionsAsync()
        {
            var peso = await _assetRepository.GetAssetByNameAsync("Peso Argentino")
                ?? throw new BusinessRuleException("Peso Argentino not found");
            var dolar = await _assetRepository.GetAssetByNameAsync("Dolar Estadounidense")
                ?? throw new BusinessRuleException("Dolar Estadounidense not found");
            return new List<AssetDTO>
            {
                new() { Id = peso.Id, Name = peso.Name, Symbol = peso.Symbol, AssetTypeName = peso.AssetType.Name },
                new() { Id = dolar.Id, Name = dolar.Name, Symbol = dolar.Symbol, AssetTypeName = dolar.AssetType.Name }
            };
        }

        public async Task<IEnumerable<AssetDTO>> GetReferenceAssetsAsync(int userId)
        {
            var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(userId);
            return referenceAssets.Select(a => new AssetDTO
            {
                Id = a.AssetId, Name = a.Asset.Name, Symbol = a.Asset.Symbol,
                AssetTypeName = a.Asset.AssetType.Name,
                IsReference = a.isReference, IsMainReference = a.isMainReference
            });
        }

        public async Task AssignAssetToUserAsync(int userId, int assetId)
        {
            await _asset_UserRepository.AssignAssetToUserAsync(userId, assetId);
        }

        public async Task UnassignAssetFromUserAsync(int userId, int assetId)
        {
            var isUsed = await _asset_UserRepository.IsAssetUserInUseAsync(userId, assetId);
            if (isUsed) throw new BusinessRuleException("Asset is used in transactions");
            await _asset_UserRepository.UnassignAssetToUserAsync(userId, assetId);
        }

        public async Task UpdateReferenceAsync(int userId, AssetDTO dto)
        {
            var asset = await _assetRepository.GetByIdAsync(dto.Id)
                ?? throw new NotFoundException("Asset not found");
            var userAsset = await _asset_UserRepository.GetUserAssetAsync(userId, dto.Id)
                ?? throw new NotFoundException("User asset not found");

            if (dto.IsReference)
            {
                var referenceAssets = await _asset_UserRepository.GetReferenceAssetsAsync(userId);
                if (referenceAssets.Count() >= 3) throw new BusinessRuleException("Only 3 reference assets allowed");
                if (!referenceAssets.Any()) userAsset.isMainReference = true;
            }
            else
            {
                userAsset.isMainReference = false;
            }

            userAsset.isReference = dto.IsReference;
            await _asset_UserRepository.UpdateAsync(userAsset);
        }

        public async Task UpdateMainReferenceAsync(int userId, AssetDTO dto)
        {
            var asset = await _assetRepository.GetByIdAsync(dto.Id)
                ?? throw new NotFoundException("Asset not found");
            var userAsset = await _asset_UserRepository.GetUserAssetAsync(userId, dto.Id)
                ?? throw new NotFoundException("User asset not found");

            if (!userAsset.isReference) throw new BusinessRuleException("Asset is not a reference");

            userAsset.isReference = dto.IsReference;
            await _asset_UserRepository.UpdateAsync(userAsset);

            var mainReferenceAsset = await _asset_UserRepository.GetMainReferenceAssetAsync(userId);
            if (mainReferenceAsset != null && dto.IsMainReference && mainReferenceAsset.AssetId != dto.Id)
            {
                mainReferenceAsset.isMainReference = false;
                await _asset_UserRepository.UpdateAsync(mainReferenceAsset);
            }

            userAsset.isMainReference = true;
            await _asset_UserRepository.UpdateAsync(userAsset);
        }
    }
}
