using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JazFinanzasApp.Functions
{
    // La preparación de madrugada de las devoluciones del asistente (plan-devoluciones-llm-v2.md, D6). Se despierta a las 08:00 y a las
    // 08:30 UTC (5:00 y 5:30 de Argentina, que no tiene horario de verano) y le pide a la agenda del backend que prepare lo que falte.
    // Dos corridas porque el plan de consumo limita el tiempo de cada ejecución: la agenda tiene un presupuesto de tiempo y la segunda
    // retoma lo que la primera dejó. Qué toca y para quién lo decide la agenda; esta función no tiene lógica propia.
    public class AssistantNightlyFunction
    {
        // NCRONTAB: segundo minuto hora día mes día-de-semana.
        public const string Schedule = "0 0,30 8 * * *";

        private readonly IAssistantSchedulerService _scheduler;
        private readonly ILogger<AssistantNightlyFunction> _logger;

        public AssistantNightlyFunction(IAssistantSchedulerService scheduler, ILogger<AssistantNightlyFunction> logger)
        {
            _scheduler = scheduler;
            _logger = logger;
        }

        [Function("AssistantNightlyPreparation")]
        public async Task Run([TimerTrigger(Schedule)] TimerInfo timer, CancellationToken cancellationToken)
        {
            var result = await _scheduler.RunAsync(cancellationToken);

            var summary = "Preparación de madrugada: {Status}. Preparadas {Prepared}, salteadas {Skipped}, fallidas {Failed}. {Notes}";
            if (result.Status == AssistantSchedulerRunStatus.Failed)
                _logger.LogError(summary, result.Status, result.Prepared, result.Skipped, result.Failed, result.Notes);
            else
                _logger.LogInformation(summary, result.Status, result.Prepared, result.Skipped, result.Failed, result.Notes);
        }
    }
}
