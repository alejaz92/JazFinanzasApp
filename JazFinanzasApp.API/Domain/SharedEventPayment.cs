using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEventPayment : BaseEntity
    {
        [Required]
        [ForeignKey("SharedEventId")]
        public int SharedEventId { get; set; }
        public SharedEvent SharedEvent { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [ForeignKey("AssetId")]
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // null = usuario
        [ForeignKey("FromPersonId")]
        public int? FromPersonId { get; set; }
        public Person? FromPerson { get; set; }

        // null = usuario
        [ForeignKey("ToPersonId")]
        public int? ToPersonId { get; set; }
        public Person? ToPerson { get; set; }

        // requerido si el pago involucra al usuario (cuenta donde entró/salió, o pivote en compensación interna)
        [ForeignKey("AccountId")]
        public int? AccountId { get; set; }
        public Account? Account { get; set; }

        [Required]
        public bool IsInternalCompensation { get; set; } = false;

        public string? Notes { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<SharedEventPaymentAllocation> Allocations { get; set; }
    }
}
