using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    // Un hallazgo del asistente: algo fuera de lo normal que una regla fija no prevé. Lo producen las
    // devoluciones y la revisión semanal, y aparece en la bandeja de pendientes
    // (plan-devoluciones-llm.md, Flujo 2 y D8b).
    public class AssistantFinding : BaseEntity
    {
        [Required]
        [ForeignKey("ReviewId")]
        public int ReviewId { get; set; }
        public AssistantReview Review { get; set; }

        // Redundante con Review.UserId a propósito: la bandeja, el descarte por Fingerprint y el
        // chequeo de dueño del PUT leen hallazgos por usuario sin pasar por la pasada.
        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Detail { get; set; }

        // Categoría a la que se refiere, para armar el link desde la bandeja.
        [ForeignKey("TransactionClassId")]
        public int? TransactionClassId { get; set; }
        public TransactionClass? TransactionClass { get; set; }

        // Categoría + tipo de hallazgo normalizados: evita repetir el mismo hallazgo en el mismo mes,
        // aunque se haya descartado (D15).
        [Required]
        [MaxLength(200)]
        public string Fingerprint { get; set; }

        // El control de cifras también se aplica a los hallazgos.
        public string? UnverifiedFiguresJson { get; set; }

        // Active / Dismissed / Expired / Repeated, ver AssistantFindingStatus.
        [Required]
        [MaxLength(10)]
        public string Status { get; set; }

        // 14 días o fin de mes, lo que llegue primero. Un hallazgo vencido pasa a Expired al leerse.
        [Required]
        public DateTime ExpiresAt { get; set; }

        // +1 útil / -1 no útil. Null mientras no se califique.
        public int? Rating { get; set; }
    }

    public static class AssistantFindingStatus
    {
        public const string Active = "Active";
        public const string Dismissed = "Dismissed";
        public const string Expired = "Expired";

        // Repeated: el asistente lo devolvió pero ya había uno con el mismo Fingerprint en el mes, así que no se
        // muestra. Se guarda igual para que la revisión de calidad vea cuánto insiste con lo mismo (D15). Se
        // agregó para eso, en vez de una columna nueva en AssistantReview (que habría pedido otra migración).
        public const string Repeated = "Repeated";

        public static bool IsValid(string? value) =>
            value == Active || value == Dismissed || value == Expired || value == Repeated;
    }
}
