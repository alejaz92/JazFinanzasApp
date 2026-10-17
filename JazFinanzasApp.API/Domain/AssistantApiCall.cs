using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Registro de gasto: una fila por llamada a la API del modelo, salga bien o mal, porque las
    // fallidas también se pagan (plan-devoluciones-llm.md, D11). El tope mensual se calcula sumando
    // CostUsd de estas filas.
    public class AssistantApiCall : BaseEntity
    {
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        // Review / WeeklyScan / Eval, ver AssistantApiCallPurpose.
        [Required]
        [MaxLength(12)]
        public string Purpose { get; set; }

        [Required]
        [MaxLength(60)]
        public string Model { get; set; }

        [MaxLength(40)]
        public string? PromptVersion { get; set; }

        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public long CacheReadTokens { get; set; }
        public long CacheWriteTokens { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,6)")]
        public decimal CostUsd { get; set; }

        [MaxLength(30)]
        public string? StopReason { get; set; }

        public bool Succeeded { get; set; }

        // La pasada que generó la llamada. Nulable: una llamada puede fallar antes de crear la fila.
        [ForeignKey("ReviewId")]
        public int? ReviewId { get; set; }
        public AssistantReview? Review { get; set; }
    }

    public static class AssistantApiCallPurpose
    {
        public const string Review = "Review";
        public const string WeeklyScan = "WeeklyScan";
        public const string Eval = "Eval";

        public static bool IsValid(string? value) =>
            value == Review || value == WeeklyScan || value == Eval;
    }
}
