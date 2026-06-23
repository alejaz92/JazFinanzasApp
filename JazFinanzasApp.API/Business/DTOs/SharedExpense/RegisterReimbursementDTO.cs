using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedExpense
{
    public class RegisterReimbursementDTO
    {
        [Required]
        public int SplitId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        public int TransactionClassId { get; set; }
    }
}
