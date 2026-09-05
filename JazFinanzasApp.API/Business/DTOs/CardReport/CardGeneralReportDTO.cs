using JazFinanzasApp.API.Business.DTO.CardTransaction;

namespace JazFinanzasApp.API.Business.DTO.CardReport
{
    public class CardGeneralReportDTO
    {
        // Corrección 2026-09-05: MonthlySeries llega convertida a ReferenceAssetSymbol (el "Peso"/
        // "Dollar" de cada CardMonthAmountDTO sigue diciendo de qué moneda salió el gasto — eso no
        // cambia — pero el número ya está expresado en la moneda de referencia elegida). Los colores
        // de cada moneda de origen viajan acá para pintar el encabezado de cada gráfico con el mismo
        // criterio que Patrimonio → General (NetWorthTotalDTO.Color).
        public string ReferenceAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetSymbol { get; set; } = string.Empty;
        public string PesoAssetColor { get; set; } = string.Empty;
        public string DollarAssetSymbol { get; set; } = string.Empty;
        public string DollarAssetColor { get; set; } = string.Empty;

        public List<CardMonthlySeriesPointDTO> MonthlySeries { get; set; } = new();
        public List<CardTransactionPaymentListDTO> CurrentMonthSummary { get; set; } = new();
    }

    public class CardMonthlySeriesPointDTO
    {
        public DateTime Month { get; set; }
        public List<CardMonthAmountDTO> Cards { get; set; } = new();
    }

    public class CardMonthAmountDTO
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public decimal PesosAmount { get; set; }
        public decimal DollarsAmount { get; set; }
    }
}
