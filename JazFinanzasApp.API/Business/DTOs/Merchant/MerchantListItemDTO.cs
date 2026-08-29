namespace JazFinanzasApp.API.Business.DTO.Merchant
{
    public class MerchantListItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsConfirmed { get; set; }
        // Movimientos + consumos de tarjeta atribuidos a este comercio.
        public int Volume { get; set; }
    }
}
