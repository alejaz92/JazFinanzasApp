using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventMovementAddDTO
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int TransactionClassId { get; set; }

        [Required]
        public int AssetId { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        // null = pagó el usuario
        public int? PayerPersonId { get; set; }

        [Required]
        public List<SharedEventMovementShareInputDTO> Shares { get; set; } = new();

        // Requerido si PayerPersonId es null; prohibido si no
        public SharedEventMovementPaymentInputDTO? Payment { get; set; }

        public string? Notes { get; set; }
    }
}
