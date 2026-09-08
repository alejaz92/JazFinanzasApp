namespace JazFinanzasApp.API.Business.DTO.Dashboard
{
    // Sección 5.3: una fila por pendiente, ordenada por urgencia (mismo orden en que las arma
    // DashboardService.BuildPendingAsync: tarjetas, reintegros, eventos, viajes). `Kind` es el
    // contrato con el frontend para elegir el ícono y la acción ("Pagar" / "Ver tarjeta" /
    // "Ver evento" / "Cargar gasto") — CardDue / PendingReimbursement / OpenSharedEvent /
    // TripWithoutRecentExpense.
    public class DashboardPendingItemDTO
    {
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public decimal? Amount { get; set; }
        public string? AssetSymbol { get; set; }
        public DateTime? Date { get; set; }

        // Id de la tarjeta / reintegro (CardTransactionId) / evento / viaje, según Kind — para armar
        // el link "Ver X" sin otra consulta.
        public int? LinkId { get; set; }
    }
}
