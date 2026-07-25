using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent.Import
{
    public class SharedEventImportParseDTO
    {
        [Required]
        public string CsvContent { get; set; } = string.Empty;

        // nombre de la columna del CSV que corresponde al usuario (null = todavía no se sabe;
        // sin este dato no se pueden calcular sugerencias de Transaction/CardTransaction existentes)
        public string? CurrentUserMemberName { get; set; }
    }
}
