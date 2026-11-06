using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Si un mes está listo para su cierre (plan-devoluciones-llm-v2.md, D4). Con el gasto por fecha de compra el pago del resumen de la
    // tarjeta ya no importa; lo que sí tiene que estar al día es:
    //  - la cotización del último día del mes de todo lo que había en cartera ese día (para el patrimonio y las inversiones), y
    //  - los cobros de bonos con fecha de pago en ese mes, registrados.
    // Los consumos y gastos no se esperan: no hay forma de saber si falta uno.
    //
    // Missing son los textos de lo que falta, tal como viajan al asistente y se muestran en pantalla ("armada sin…").
    public record AssistantMonthReadiness(bool Ready, IReadOnlyList<string> Missing, bool DeadlineReached)
    {
        // Día del mes siguiente a partir del cual el cierre se prepara aunque falte algo (anotando qué faltaba).
        public const int DeadlineDay = 7;

        // Se prepara si está todo o si ya llegó el tope.
        public bool ShouldPrepare => Ready || DeadlineReached;

        // Pura: el mes está cerrado y, con faltantes, se espera hasta el día 7 del mes siguiente inclusive.
        public static AssistantMonthReadiness Decide(IReadOnlyList<string> missing, DateTime month, DateTime today)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var deadline = monthStart.AddMonths(1).AddDays(DeadlineDay - 1);
            var deadlineReached = today.Date >= deadline;
            return new AssistantMonthReadiness(missing.Count == 0, missing, deadlineReached);
        }
    }

    public interface IAssistantMonthReadinessService
    {
        // Chequea el mes `month` (cualquier día del mes) al día `today`.
        Task<AssistantMonthReadiness> CheckAsync(int userId, DateTime month, DateTime today);
    }

    public class AssistantMonthReadinessService : IAssistantMonthReadinessService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBondCollectionService _bondCollections;

        public AssistantMonthReadinessService(ITransactionRepository transactionRepository, IBondCollectionService bondCollections)
        {
            _transactionRepository = transactionRepository;
            _bondCollections = bondCollections;
        }

        public async Task<AssistantMonthReadiness> CheckAsync(int userId, DateTime month, DateTime today)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var lastDay = monthStart.AddMonths(1).AddDays(-1);

            var withoutQuote = (await _transactionRepository.GetHeldAssetsWithoutQuoteAsync(userId, lastDay)).ToList();
            var pendingBonds = (await _bondCollections.GetPendingAsync(userId))
                .Where(c => c.PaymentDate >= monthStart && c.PaymentDate <= lastDay)
                .OrderBy(c => c.PaymentDate)
                .ToList();

            return AssistantMonthReadiness.Decide(BuildMissing(withoutQuote.Select(a => a.AssetSymbol).ToList(), pendingBonds, lastDay), month, today);
        }

        // Pura — testeable sin mocks.
        public static List<string> BuildMissing(
            IReadOnlyCollection<string> symbolsWithoutQuote,
            IReadOnlyCollection<Business.DTO.BondCollection.BondCollectionPendingDTO> pendingBonds,
            DateTime lastDay)
        {
            var missing = new List<string>();
            if (symbolsWithoutQuote.Count > 0)
            {
                var symbols = string.Join(", ", symbolsWithoutQuote);
                missing.Add($"Falta la cotización del {lastDay:dd/MM} de: {symbols}. Las inversiones y el patrimonio se valuaron con la última cotización disponible.");
            }
            foreach (var bond in pendingBonds)
                missing.Add($"Falta registrar el cobro de {bond.Symbol} del {bond.PaymentDate:dd/MM}: los ingresos del mes pueden estar incompletos.");
            return missing;
        }
    }
}
