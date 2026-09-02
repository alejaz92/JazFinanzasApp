using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class Asset : BaseEntity
    {
        [Required]
        public string Name {  get; set; }
        [Required]
        public string Symbol {  get; set; }
        [Required]
        public int AssetTypeId { get; set; }
        [Required]
        public AssetType AssetType { get; set; }
        public string Color { get; set; }

        // Moneda a la que está atado el valor del activo — no en qué moneda cotiza en pantalla,
        // sino qué lo hace subir o bajar cuando se mueve el tipo de cambio. Un CEDEAR cotiza en
        // pesos pero está atado al dólar; un Boncer CER cotiza igual que un bono en dólares pero
        // está atado al peso. `null` es una respuesta válida: el activo no sigue a ninguna moneda
        // y tiene precio propio (Bitcoin, un FCI mixto).
        [ForeignKey("LinkedCurrencyAssetId")]
        public int? LinkedCurrencyAssetId { get; set; }
        public Asset? LinkedCurrency { get; set; }



    }
}
