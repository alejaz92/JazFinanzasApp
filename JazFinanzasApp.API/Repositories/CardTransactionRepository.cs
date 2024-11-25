using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
using Microsoft.EntityFrameworkCore;

namespace JazFinanzasApp.API.Repositories
{
    public class CardTransactionRepository : GenericRepository<CardTransaction>, ICardTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public CardTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }    
        public async Task<IEnumerable<CardTransactionsPendingDTO>> GetPendingCardTransactionsAsync(int userId)
        {
            var today = DateTime.Today;
            today = new DateTime(today.Year, today.Month, 1);
            DateTime nullDate = new DateTime(0001, 01, 01, 00, 00, 00, 0000000);

            var pendingMovements = await _context.CardTransactions
                .Include(cm => cm.Card)
                .Include(cm => cm.TransactionClass)
                .Include(cm => cm.Asset)
                .Where(cm => cm.Card.UserId == userId && (
                    (cm.Repeat == "YES" &&  (cm.LastInstallment  ==   nullDate || cm.LastInstallment == null )) ||
                    (cm.Repeat == "NO" && cm.FirstInstallment <= today && cm.LastInstallment >= today) 
                    
                ))
                .Select(cm => new CardTransactionsPendingDTO
                {
                    Id = cm.Id,
                    Date = cm.Date,
                    Card = cm.Card.Name,
                    TransactionClass = cm.TransactionClass.Description,
                    Detail = cm.Detail,
                    Installments = cm.Repeat == "YES" ? "Recurrente" : cm.Installments.ToString(),
                    Asset = cm.Asset.Name,
                    TotalAmount = cm.TotalAmount,
                    FirstInstallment = cm.FirstInstallment,
                    LastInstallment = cm.Repeat == "YES" ? "NA" : (cm.LastInstallment.HasValue ? cm.LastInstallment.Value.ToString("MM/yyyy") : "NA"),
                    InstallmentAmount = cm.InstallmentAmount
                })
                .OrderBy(cm => cm.Card)
                .ThenBy(cm => cm.Asset)
                .ThenBy(cm => cm.Date)
                .ToListAsync();

            return pendingMovements;
        }

        public async Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId)
        {
            var firstDayOfMonth = new DateTime(paymentMonth.Year, paymentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return await _context.CardTransactions
                .Include(cm => cm.Card)
                .Include(cm => cm.TransactionClass)
                .Include(cm => cm.Asset)
                .Where(cm => cm.CardId == cardId
                    && cm.UserId == userId
                    && (
                        (
                            cm.Repeat == "NO"
                            && cm.FirstInstallment <=lastDayOfMonth
                            && cm.LastInstallment >= firstDayOfMonth) 
                        ||
                        (
                            cm.Repeat == "YES"
                            && cm.FirstInstallment <= firstDayOfMonth))
                        )                
                .ToListAsync();
        }

        
    }
}
