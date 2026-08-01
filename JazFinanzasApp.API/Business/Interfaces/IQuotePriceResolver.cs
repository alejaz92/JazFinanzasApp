namespace JazFinanzasApp.API.Business.Interfaces
{
    // Resuelve la cotización que se guarda en Transaction.QuotePrice. Toda transacción que se persista
    // debe tener este campo cargado: los reportes dividen por él (Amount / QuotePrice) para convertir a
    // la moneda de referencia, y un null hace que la fila se descarte en silencio en los resúmenes
    // mensuales o se cuente como si ya estuviera en dólares en el reporte de Viajes.
    public interface IQuotePriceResolver
    {
        Task<decimal> ResolveAsync(int assetId, DateTime date);
    }
}
