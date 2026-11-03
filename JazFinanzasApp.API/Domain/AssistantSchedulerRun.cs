using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Domain
{
    // Una corrida de la preparación de madrugada de las devoluciones (plan-devoluciones-llm-v2.md, D6). No es de ningún usuario:
    // la corrida recorre a todos los habilitados. Sirve para que la tarjeta de Inicio avise si la de anoche no corrió o falló, y
    // para la guía de operación.
    public class AssistantSchedulerRun : BaseEntity
    {
        public DateTime StartedAt { get; set; }

        // Null mientras corre.
        public DateTime? FinishedAt { get; set; }

        // Running / Completed / Partial / Failed, ver AssistantSchedulerRunStatus.
        [Required]
        [MaxLength(12)]
        public string Status { get; set; } = AssistantSchedulerRunStatus.Running;

        public int Prepared { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }

        // Qué se preparó, qué se salteó y por qué.
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }

    public static class AssistantSchedulerRunStatus
    {
        public const string Running = "Running";
        public const string Completed = "Completed";
        // Se quedó sin tiempo y dejó pasadas para la próxima corrida.
        public const string Partial = "Partial";
        public const string Failed = "Failed";
    }
}
