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

        // ACCOUNT: el banco acredito en una cuenta propia. CARD: quedo como saldo a favor de la tarjeta.
        // Ver CardTransactionDiscountCreditTarget. Si viene vacio se asume ACCOUNT, para que un cliente
        // viejo que todavia no conoce el campo (el frontend, hasta la Fase 6) siga funcionando igual.
        public string? CreditTarget { get; set; }

        // Obligatoria solo cuando CreditTarget es ACCOUNT; con CARD no entra plata a ninguna cuenta.
        public int? AccountId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Notes { get; set; }
    }
}
