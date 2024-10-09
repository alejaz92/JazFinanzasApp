using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.Account_AssetType;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class Account_AssetTypeRepository : GenericRepository<Account_AssetType>, IAccount_AssetTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public Account_AssetTypeRepository(ApplicationDbContext context) : base(context)
        {    
            _context = context;
        }

        public async Task<IEnumerable<Account_AssetTypeDTO>> GetAssetTypes(int id)
        {
            var assetTypes = await _context.AssetTypes.ToListAsync();

            var account_AssetType = await _context.Account_AssetTypes
                .Where(x => x.AccountId == id)
                .ToListAsync();

            var result = assetTypes.Select(assetType => new Account_AssetTypeDTO
            {
                Id = assetType.Id,
                Name = assetType.Name,
                IsSelected = account_AssetType.Any(x => x.AssetTypeId == assetType.Id)
            }).ToList();

            return result;

        }

        public async Task<bool> AssignAssetTypesToAccountAsync(int accountId, List<Account_AssetTypeDTO> assetTypes)
        {
            // Paso 1: Obtener los registros actuales de tipos de activos para la cuenta
            var currentAssetTypes = await _context.Account_AssetTypes
                .Where(x => x.AccountId == accountId)
                .ToListAsync();

            // Paso 2: Eliminar los registros actuales
            _context.Account_AssetTypes.RemoveRange(currentAssetTypes);

            // Paso 3: Insertar los nuevos tipos de activos seleccionados
            var newAssetTypes = assetTypes
                .Where(x => x.IsSelected)
                .Select(x => new Account_AssetType
                {
                    AccountId = accountId,
                    AssetTypeId = x.Id
                });

            await _context.Account_AssetTypes.AddRangeAsync(newAssetTypes);

            // Paso 4: Guardar los cambios
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
