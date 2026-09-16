using JazFinanzasApp.API.Business.DTO.ReportFavorite;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Services
{
    public class ReportFavoriteService : IReportFavoriteService
    {
        private readonly IReportFavoriteRepository _reportFavoriteRepository;

        public ReportFavoriteService(IReportFavoriteRepository reportFavoriteRepository)
        {
            _reportFavoriteRepository = reportFavoriteRepository;
        }

        public async Task<IEnumerable<ReportFavoriteDTO>> GetAllForUserAsync(int userId)
        {
            var favorites = await _reportFavoriteRepository.GetByUserIdAsync(userId);
            return favorites.OrderBy(f => f.Order).Select(ToDto);
        }

        public async Task<ReportFavoriteDTO> CreateAsync(int userId, string reportKey)
        {
            var existing = await _reportFavoriteRepository.GetByReportKeyAsync(reportKey, userId);
            if (existing != null) throw new BusinessRuleException("Report already marked as favorite");

            var favorites = await _reportFavoriteRepository.GetByUserIdAsync(userId);
            var nextOrder = favorites.Any() ? favorites.Max(f => f.Order) + 1 : 0;

            var created = await _reportFavoriteRepository.AddAsyncReturnObject(new ReportFavorite
            {
                ReportKey = reportKey,
                Order = nextOrder,
                UserId = userId
            });

            return ToDto(created);
        }

        public async Task DeleteAsync(int userId, int id)
        {
            await GetOwnedFavoriteAsync(userId, id);
            await _reportFavoriteRepository.DeleteAsync(id);
        }

        public async Task ReorderAsync(int userId, List<int> orderedIds)
        {
            var favorites = (await _reportFavoriteRepository.GetByUserIdAsync(userId)).ToList();
            if (orderedIds.Count != favorites.Count || favorites.Any(f => !orderedIds.Contains(f.Id)))
                throw new BusinessRuleException("El nuevo orden tiene que incluir exactamente los favoritos actuales");

            for (var i = 0; i < orderedIds.Count; i++)
            {
                var favorite = favorites.Single(f => f.Id == orderedIds[i]);
                favorite.Order = i;
                await _reportFavoriteRepository.UpdateAsync(favorite);
            }
        }

        private async Task<ReportFavorite> GetOwnedFavoriteAsync(int userId, int id)
        {
            var favorite = await _reportFavoriteRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Favorite not found");
            if (favorite.UserId != userId) throw new UnauthorizedDomainException();
            return favorite;
        }

        private static ReportFavoriteDTO ToDto(ReportFavorite favorite) => new ReportFavoriteDTO
        {
            Id = favorite.Id,
            ReportKey = favorite.ReportKey,
            Order = favorite.Order
        };
    }
}
