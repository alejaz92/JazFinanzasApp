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
                AssetTypeId = assetType.Id,
                AssetTypeName = assetType.Name,
                IsSelected = account_AssetType.Any(x => x.AssetTypeId == assetType.Id)
            }).ToList();

            return result;

        }
    }
}
