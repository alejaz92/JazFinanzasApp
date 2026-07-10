using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Sugerencia descartada por el usuario para un viaje: no se vuelve a ofrecer.
    // Referencia exactamente uno de los dos movimientos (TransactionId o CardTransactionId).
    public class TripSuggestionDismissal : BaseEntity
    {
        [Required]
        [ForeignKey("TripId")]
        public int TripId { get; set; }
        public Trip Trip { get; set; }

        [ForeignKey("TransactionId")]
        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        [ForeignKey("CardTransactionId")]
        public int? CardTransactionId { get; set; }
        public CardTransaction? CardTransaction { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
