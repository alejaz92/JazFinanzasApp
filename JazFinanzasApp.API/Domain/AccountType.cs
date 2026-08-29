namespace JazFinanzasApp.API.Domain
{
    // Tipo de cuenta (sección 7, plan-rediseno-reportes.md): separa "disponible" de "invertido"
    // en el reporte de Patrimonio. Opcional — una cuenta sin tipo asignado sigue contando como
    // líquida por default (ver Account.CountsAsLiquid).
    public static class AccountType
    {
        public const string Cash = "CASH";
        public const string Bank = "BANK";
        public const string Wallet = "WALLET";
        public const string Investment = "INVESTMENT";
        public const string Other = "OTHER";

        public static bool IsValid(string? value) =>
            value == Cash || value == Bank || value == Wallet || value == Investment || value == Other;
    }
}
