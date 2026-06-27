using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.CardTransactionDiscount
{
    public class CardTransactionDiscountAddDTO
    {
        [Required]
        public int CardTransactionId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Notes { get; set; }
    }
}
