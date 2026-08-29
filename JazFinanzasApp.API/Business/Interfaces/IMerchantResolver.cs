namespace JazFinanzasApp.API.Business.Interfaces
{
    public interface IMerchantResolver
    {
        // Devuelve el Id del comercio para ese detalle, creándolo si hace falta. Null si el
        // detalle normaliza a vacío (T7 — un detalle vacío no crea comercio).
        Task<int?> ResolveAsync(int userId, string? detail);
    }
}
