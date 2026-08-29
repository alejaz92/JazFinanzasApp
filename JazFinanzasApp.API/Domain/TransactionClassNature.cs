namespace JazFinanzasApp.API.Domain
{
    // Naturaleza de una categoría (D-2 / sección 8, plan-rediseno-reportes.md): habilita el
    // reporte "Fijos vs variables" y la tasa de ahorro sobre lo realmente ingresado. Opcional —
    // una categoría sin naturaleza asignada simplemente no entra en esos reportes.
    public static class TransactionClassNature
    {
        public const string Essential = "ESSENTIAL";
        public const string Discretionary = "DISCRETIONARY";
        public const string Saving = "SAVING";

        public static bool IsValid(string? value) => value == Essential || value == Discretionary || value == Saving;
    }
}
