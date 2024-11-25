using Microsoft.Identity.Client;

namespace JazFinanzasApp.API.Models.DTO.transactionClass
{
    public class TransactionClassDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string IncExp { get; set; }
    }
}
