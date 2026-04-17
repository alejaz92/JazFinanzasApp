using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Infrastructure.Data;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Infrastructure.Repositories
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


        public async Task<Account_AssetType> GetAccount_AssetTypeByAccountIdAndAssetTypeNameAsync(int accountId, string assetTypeName)
        {
            return await _context.Account_AssetTypes
                .Include(a => a.Account)
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.AssetType.Name == assetTypeName);
        }
    }
}
