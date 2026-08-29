namespace JazFinanzasApp.API.Business.DTO.Merchant
{
    // "Movimiento" en sentido amplio: une Transaction y CardTransaction en una sola lista para
    // la pantalla de "ver movimientos" de un comercio (Fase 9, plan-rediseno-reportes.md).
    public class MerchantMovementDTO
    {
        public int Id { get; set; }
        // "Transaction" o "CardTransaction" — distingue a qué endpoint de reasignación llamar.
        public string Source { get; set; }
        public DateTime Date { get; set; }
        public string? Detail { get; set; }
        public decimal Amount { get; set; }
    }
}
