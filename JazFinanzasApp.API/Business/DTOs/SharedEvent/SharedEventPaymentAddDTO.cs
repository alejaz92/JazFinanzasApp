using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventPaymentAddDTO
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int AssetId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        // null = el usuario
        public int? FromPersonId { get; set; }

        // null = el usuario
        public int? ToPersonId { get; set; }

        // requerido si el pago involucra al usuario (cuenta donde entró/salió, o pivote en compensación interna)
        public int? AccountId { get; set; }

        public bool IsInternalCompensation { get; set; }

        public string? Notes { get; set; }

        // sin especificar: imputación default FIFO (D2); con ellas: override manual validado contra la misma ecuación
        public List<SharedEventPaymentAllocationInputDTO>? Allocations { get; set; }
    }
}
