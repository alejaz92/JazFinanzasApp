using JazFinanzasApp.API.Data;
using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.CardTransaction;
using JazFinanzasApp.API.Models.DTO.Report;
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

            var pendingTransactions = await _context.CardTransactions
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
                    AssetSymbol = cm.Asset.Symbol,
                    TotalAmount = cm.TotalAmount,
                    FirstInstallment = cm.FirstInstallment,
                    LastInstallment = cm.Repeat == "YES" ? "NA" : (cm.LastInstallment.HasValue ? cm.LastInstallment.Value.ToString("MM/yyyy") : "NA"),
                    InstallmentAmount = cm.InstallmentAmount
                })
                .OrderBy(cm => cm.Date)
                .ThenBy(cm => cm.Card)
                .ThenBy(cm => cm.Asset)
                .ToListAsync();

            return pendingTransactions;
        }

        public async Task<IEnumerable<CardTransaction>> GetCardTransactionsToPay(int cardId, DateTime paymentMonth, int userId)
        {
            var firstDayOfMonth = new DateTime(paymentMonth.Year, paymentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return await _context.CardTransactions
                .Include(cm => cm.Card)
                .Include(cm => cm.TransactionClass)
                .Include(cm => cm.Asset)
                .Where(cm => cardId == 0 || cm.CardId == cardId)
                .Where(cm => cm.UserId == userId
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


        public async Task<IEnumerable<CardGraphDTO>> GetCardStats(int? cardId, string Asset, int userId)
        {
            var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var startDate = today.AddMonths(-6);
            var endDate = today.AddMonths(5);

            // Traer solo los datos necesarios desde la base de datos
            var transactions = await _context.CardTransactions
                .Include(ct => ct.Asset)
                .Where(ct => ct.Asset.Name == Asset)
                .Where(ct => ct.UserId == userId &&
                             (cardId == 0 || ct.CardId == cardId))
                .ToListAsync();

            // Realizar la expansión de meses en memoria
            var expandedTransactions = transactions
                .SelectMany(ct =>
                {
                    var firstMonth = ct.FirstInstallment;
                    var lastMonth = ct.Repeat == "YES" || ct.LastInstallment == null
                        ? endDate
                        : ct.LastInstallment.Value;

                    var totalMonths = GetMonthDifference(firstMonth, lastMonth) + 1;

                    return Enumerable.Range(0, totalMonths)
                        .Select(i => new
                        {
                            Month = firstMonth.AddMonths(i),
                            InstallmentAmount = ct.InstallmentAmount
                        })
                        .Where(x => x.Month >= startDate && x.Month <= endDate);
                });

            // Generar rango completo de meses
            var allMonths = Enumerable.Range(0, GetMonthDifference(startDate, endDate) + 1)
                .Select(i => startDate.AddMonths(i));

            // Combinar datos existentes con el rango completo de meses
            var result = allMonths
                .GroupJoin(expandedTransactions,
                    month => month,
                    transaction => transaction.Month,
                    (month, transactions) => new CardGraphDTO
                    {
                        Month = month,
                        Amount = transactions.Sum(x => x.InstallmentAmount) // Si no hay transacciones, el valor será 0
                    })
                .ToList();

            return result;

        }

        private int GetMonthDifference(DateTime startDate, DateTime endDate)
        {
            return ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
        }




    }

}
