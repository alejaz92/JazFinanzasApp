namespace JazFinanzasApp.API.Business.DTO.Account
{
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // CASH / BANK / WALLET / INVESTMENT / OTHER — ver Domain.AccountType.
        public string? Type { get; set; }
        // Nullable a propósito: null = "no se especificó" (crear → default true, editar → no
        // toca el valor existente), a diferencia de false que es una elección explícita.
        public bool? CountsAsLiquid { get; set; }
    }
}
