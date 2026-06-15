using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class SharedExpenseAddDTO
    {
        [Required]
        public int TransactionId { get; set; }

        public string? Notes { get; set; }

        [Required]
        public List<SplitInputDTO> Splits { get; set; } = new();
    }

    public class SplitInputDTO
    {
        [Required]
        public int PersonId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }
    }
}
