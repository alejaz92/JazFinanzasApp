namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventPaymentPreviewItemDTO
    {
        // "Credit" = ítem a favor del usuario (splits del motor V1); "Debt" = deuda propia del usuario
        public string Kind { get; set; }
        public int? SplitId { get; set; }
        public int? ShareId { get; set; }
        public int MovementId { get; set; }
        public string MovementDescription { get; set; }
        public DateTime MovementDate { get; set; }

        // quién debía o a quién se le debía (null = el usuario, solo aplica a Credit de deudor cruzado; en Debt siempre es el usuario)
        public int? PersonId { get; set; }
        public string? PersonName { get; set; }

        public decimal Amount { get; set; }
        public decimal PendingBefore { get; set; }
        public decimal PendingAfter { get; set; }
    }
}
