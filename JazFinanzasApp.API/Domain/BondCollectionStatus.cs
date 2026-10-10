namespace JazFinanzasApp.API.Domain
{
    // Qué pasó con un pago del cronograma de un bono para una tenencia puntual (ver
    // plan-amortizaciones-bonos.md, Etapa 2 — Modelo de datos).
    // Registered: cobrado y registrado, con sus movimientos de Transaction.
    // Untracked: cobrado, pero sin movimiento identificable en el historial — solo queda marcado
    //            para que el rendimiento del bono lo cuente, sin tocar ninguna cuenta.
    // Dismissed: no hubo cobro (ej. se vendió el bono antes de la fecha de pago).
    public static class BondCollectionStatus
    {
        public const string Registered = "Registered";
        public const string Untracked = "Untracked";
        public const string Dismissed = "Dismissed";

        public static bool IsValid(string? value) =>
            value == Registered || value == Untracked || value == Dismissed;
    }
}
