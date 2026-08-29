using Microsoft.Identity.Client;

namespace JazFinanzasApp.API.Business.DTO.TransactionClass
{
    public class TransactionClassDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string IncExp { get; set; }
        public bool IsSystem { get; set; }
        public int? ParentId { get; set; }
        // ESSENTIAL / DISCRETIONARY / SAVING — ver Domain.TransactionClassNature.
        public string? Nature { get; set; }
    }
}
