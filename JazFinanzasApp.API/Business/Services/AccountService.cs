using JazFinanzasApp.API.Business.DTO.Account;
using JazFinanzasApp.API.Business.DTO.Account_AssetType;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data.QueryResults;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Business.Exceptions;

namespace JazFinanzasApp.API.Business.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAssetTypeRepository _assetTypeRepository;
        private readonly IAccount_AssetTypeRepository _account_AssetTypeRepository;

        public AccountService(
            IAccountRepository accountRepository,
            IAssetTypeRepository assetTypeRepository,
            IAccount_AssetTypeRepository account_AssetTypeRepository)
        {
            _accountRepository = accountRepository;
            _assetTypeRepository = assetTypeRepository;
            _account_AssetTypeRepository = account_AssetTypeRepository;
        }

        public async Task<IEnumerable<AccountDTO>> GetAllForUserAsync(int userId)
        {
            var accounts = await _accountRepository.GetByUserIdAsync(userId);
            return accounts.Select(a => new AccountDTO { Id = a.Id, Name = a.Name });
        }

        public async Task<AccountDTO> GetByIdAsync(int userId, int id)
        {
            var account = await _accountRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Account not found");
            if (account.UserId != userId) throw new UnauthorizedDomainException();
            return new AccountDTO { Id = account.Id, Name = account.Name };
        }

        public async Task<IEnumerable<AccountDTO>> GetByAssetTypeAsync(int userId, int assetTypeId)
        {
            var accounts = await _accountRepository.GetByAssetType(assetTypeId, userId);
            if (accounts == null) throw new NotFoundException("No accounts found");
            return accounts.Select(a => new AccountDTO { Id = a.Id, Name = a.Name });
        }

        public async Task<IEnumerable<AccountDTO>> GetByAssetTypeNameAsync(int userId, string assetTypeName)
        {
            var assetType = await _assetTypeRepository.GetByName(assetTypeName)
                ?? throw new NotFoundException("Asset type not found");
            return await GetByAssetTypeAsync(userId, assetType.Id);
        }

        public async Task CreateAccountAsync(int userId, AccountDTO dto)
        {
            var checkExists = await _accountRepository.FindAsync(a => a.Name == dto.Name && a.UserId == userId);
            if (checkExists.Any()) throw new BusinessRuleException("Account already exists");
            await _accountRepository.AddAsync(new Account { Name = dto.Name, UserId = userId });
        }

        public async Task UpdateAccountAsync(int userId, int id, AccountDTO dto)
        {
            var account = await _accountRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Account not found");
            if (account.UserId != userId) throw new UnauthorizedDomainException();
            account.UpdatedAt = DateTime.UtcNow;
            account.Name = dto.Name;
            await _accountRepository.UpdateAsync(account);
        }

        public async Task DeleteAccountAsync(int userId, int id)
        {
            var account = await _accountRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Account not found");
            if (account.UserId != userId) throw new UnauthorizedDomainException();
            var isUsed = await _accountRepository.IsAccountUsedInTransactions(id);
            if (isUsed) throw new BusinessRuleException("Account is used in transactions");
            await _accountRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Account_AssetTypeDTO>> GetAssetTypesForAccountAsync(int userId, int accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId)
                ?? throw new NotFoundException("Account not found");
            if (account.UserId != userId) throw new UnauthorizedDomainException();
            var results = await _account_AssetTypeRepository.GetAssetTypes(accountId);
            return results.Select(r => new Account_AssetTypeDTO { Id = r.Id, Name = r.Name, IsSelected = r.IsSelected });
        }

        public async Task AssignAssetTypesToAccountAsync(int userId, int accountId, List<Account_AssetTypeDTO> assetTypes)
        {
            var account = await _accountRepository.GetByIdAsync(accountId)
                ?? throw new NotFoundException("Account not found");
            if (account.UserId != userId) throw new UnauthorizedDomainException();
            if (assetTypes == null || !assetTypes.Any()) throw new BusinessRuleException("Select at least 1");
            var infraTypes = assetTypes.Select(d => new AccountAssetTypeResult { Id = d.Id, Name = d.Name, IsSelected = d.IsSelected });
            await _account_AssetTypeRepository.AssignAssetTypesToAccountAsync(accountId, infraTypes);
        }
    }
}
