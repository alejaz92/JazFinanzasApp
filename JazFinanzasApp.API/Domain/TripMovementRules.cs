namespace JazFinanzasApp.API.Domain
{
    // Regla de exclusión de Viajes: qué transacciones de cuenta NO son asociables ni sugeribles.
    // Los pagos de cuotas de tarjeta y el egreso agregado del resumen se excluyen porque el gasto
    // ya se computa por devengado desde el CardTransaction (ver plan-viajes.md). Las clases excluidas
    // replican el criterio de "egreso genuino" del reporte de Ingresos/Egresos.
    public static class TripMovementRules
    {
        public static readonly string[] ExcludedTransactionClasses =
        {
            "Gastos Tarjeta",
            "Ajuste Saldos Egreso",
            "Inversiones"
        };

        // Pagos de cuotas anteriores a la FK Transaction.CardTransactionId (sin backfill obligatorio)
        public const string LegacyCardPaymentDetailPrefix = "(Tarjeta | ";
    }
}
