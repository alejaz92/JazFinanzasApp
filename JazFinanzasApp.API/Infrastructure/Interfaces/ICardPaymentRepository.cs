using JazFinanzasApp.API.Domain;

namespace JazFinanzasApp.API.Infrastructure.Interfaces
{
    public interface ICardPaymentRepository : IGenericRepository<CardPayment>
    {
        Task<bool> IsPaymentAlreadyMadeAsync(int cardId, DateTime date);

        // Meses de resumen ya pagados de una tarjeta. Sirve para saber que cuotas de un gasto
        // ya se pagaron a precio pleno, y por lo tanto no pueden recibir un descuento que
        // el banco acredito despues.
        Task<IEnumerable<DateTime>> GetPaidMonthsAsync(int cardId);

        // Último mes de resumen pagado por tarjeta, de todas las tarjetas del usuario (Fase 10,
        // deuda de tarjeta viva — T8). Una tarjeta sin pagos registrados no aparece en el diccionario.
        Task<Dictionary<int, DateTime>> GetLastPaidMonthByCardAsync(int userId);
    }
}
