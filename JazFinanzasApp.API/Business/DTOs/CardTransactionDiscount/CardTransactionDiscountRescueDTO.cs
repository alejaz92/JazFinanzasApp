using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.CardTransactionDiscount
{
    // Rescate: el banco pasa el saldo a favor de la tarjeta a una cuenta. Puede ser total o parcial.
    public class CardTransactionDiscountRescueDTO
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }
}
