using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent.Import
{
    public class SharedEventImportMemberMappingDTO
    {
        [Required]
        public string MemberName { get; set; } = string.Empty;

        // null junto con PersonId null = esta columna soy "yo"; en cualquier otro caso, exactamente uno de los dos
        public int? PersonId { get; set; }
        public string? NewPersonName { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class SharedEventImportCategoryMappingDTO
    {
        [Required]
        public string CategoryName { get; set; } = string.Empty;

        public int? TransactionClassId { get; set; }
        public string? NewCategoryName { get; set; }
    }

    // Acciones posibles para una fila de gasto (no aplica a filas de pago, que siempre se crean si no se saltean)
    public static class SharedEventImportRowAction
    {
        public const string CreateNew = "CreateNew";
        public const string LinkExisting = "LinkExisting";
        public const string Skip = "Skip";
    }

    public class SharedEventImportRowDecisionDTO
    {
        public int RowIndex { get; set; }

        [Required]
        public string Action { get; set; } = SharedEventImportRowAction.Skip;

        // requerido si Action = LinkExisting
        public int? TransactionId { get; set; }
        public int? CardTransactionId { get; set; }

        // Action = CreateNew y el gasto lo pagó el usuario: exactamente uno de AccountId o CardId (+ Installments/FirstInstallment)
        public int? AccountId { get; set; }
        public int? CardId { get; set; }
        public int? Installments { get; set; }
        public DateTime? FirstInstallment { get; set; }
    }

    public class SharedEventImportConfirmDTO
    {
        [Required]
        public string CsvContent { get; set; } = string.Empty;

        [Required]
        public List<SharedEventImportMemberMappingDTO> MemberMappings { get; set; } = new();

        [Required]
        public List<SharedEventImportCategoryMappingDTO> CategoryMappings { get; set; } = new();

        [Required]
        public List<SharedEventImportRowDecisionDTO> RowDecisions { get; set; } = new();
    }
}
