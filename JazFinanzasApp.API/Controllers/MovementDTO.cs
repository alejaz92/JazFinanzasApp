
namespace JazFinanzasApp.API.Controllers
{
    internal class MovementDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public object Detail { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public int? MovementClassId { get; set; }
        public string MovementClassName { get; set; }

        public string MovementType { get; set; }

    }
}