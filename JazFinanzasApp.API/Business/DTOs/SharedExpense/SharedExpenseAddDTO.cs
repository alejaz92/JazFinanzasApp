using JazFinanzasApp.API.Domain;
using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class SharedExpenseAddDTO
    {
        public int? TransactionId { get; set; }

        public int? CardTransactionId { get; set; }

        public string? Notes { get; set; }

        [Required]
        public List<SplitInputDTO> Splits { get; set; } = new();
    }

    public class SplitInputDTO
    {
        public int? PersonId { get; set; }

        public SharedExpenseSplitType SplitType { get; set; } = SharedExpenseSplitType.Person;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        // Solo para SplitType = BankPromotion: permite precalcular la distribución FIFO al crear el gasto compartido de tarjeta
        public int? AccountId { get; set; }
        public DateTime? Date { get; set; }
        public int? TransactionClassId { get; set; }
    }
}
