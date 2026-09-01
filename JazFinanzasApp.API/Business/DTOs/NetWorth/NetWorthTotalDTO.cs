namespace JazFinanzasApp.API.Business.DTO.NetWorth
{
    public class NetWorthTotalDTO
    {
        public string Asset { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public decimal GrossBalance { get; set; }
        public decimal CardDebt { get; set; }
        public decimal NetBalance { get; set; }

        // T9: la fecha de cotización más vieja usada para valuar alguna tenencia de hoy —
        // si supera los 3 días hábiles, el frontend la muestra en vez de tratar el número como al día.
        public DateTime? OldestQuoteDate { get; set; }
    }
}
