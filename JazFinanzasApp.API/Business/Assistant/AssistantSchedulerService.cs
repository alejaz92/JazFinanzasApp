using System.Text;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JazFinanzasApp.API.Business.Assistant
{
    public class AssistantSchedulerOptions
    {
        public const string SectionName = "AssistantScheduler";

        // Cuánto puede durar una corrida (el plan de consumo de la Function App corta cada ejecución). No empieza una pasada
        // si no le alcanza el tiempo; las que quedan las toma la corrida siguiente.
        public TimeSpan TimeBudget { get; set; } = TimeSpan.FromMinutes(8);

        // Cuánto se cuenta que tarda una pasada (hoy unos 2 minutos sobre la base chica).
        public TimeSpan EstimatedPassDuration { get; set; } = TimeSpan.FromMinutes(2.5);

        // Si hay otra corrida en curso que empezó hace menos que esto, esta no hace nada: las dos corridas de la madrugada
        // no se pisan.
        public TimeSpan OverlapGuard { get; set; } = TimeSpan.FromMinutes(15);
    }

    public static class AssistantSchedulerRunResultStatus
    {
        // No se hizo nada porque otra corrida sigue en curso.
        public const string AlreadyRunning = "AlreadyRunning";
    }

    // RunId es 0 cuando no se creó una corrida (AlreadyRunning).
    public record AssistantSchedulerResult(int RunId, string Status, int Prepared, int Skipped, int Failed, string? Notes);

    public interface IAssistantSchedulerService
    {
        // La preparación de madrugada (plan-devoluciones-llm-v2.md, D6): para cada usuario con el asistente habilitado decide
        // qué pasadas tocan, con las mismas reglas que `reviews/latest`, y las genera. Nunca lanza: lo que sale mal queda en la
        // corrida registrada.
        Task<AssistantSchedulerResult> RunAsync(CancellationToken cancellationToken = default);
    }

    public class AssistantSchedulerService : IAssistantSchedulerService
    {
        private const int MaxNotesLength = 2000;

        private readonly IAssistantUsageRepository _usageRepository;
        private readonly IAssistantUsageService _usage;
        private readonly IAssistantReviewService _reviews;
        private readonly IAssistantMonthReadinessService _readiness;
        private readonly IAssistantSchedulerRunRepository _runs;
        private readonly AssistantSchedulerOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AssistantSchedulerService> _logger;

        public AssistantSchedulerService(
            IAssistantUsageRepository usageRepository,
            IAssistantUsageService usage,
            IAssistantReviewService reviews,
            IAssistantMonthReadinessService readiness,
            IAssistantSchedulerRunRepository runs,
            IOptions<AssistantSchedulerOptions> options,
            TimeProvider timeProvider,
            ILogger<AssistantSchedulerService> logger)
        {
            _usageRepository = usageRepository;
            _usage = usage;
            _reviews = reviews;
            _readiness = readiness;
            _runs = runs;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public async Task<AssistantSchedulerResult> RunAsync(CancellationToken cancellationToken = default)
        {
            var started = UtcNow;
            if (await _runs.AnyRunningSinceAsync(started - _options.OverlapGuard))
                return new AssistantSchedulerResult(0, AssistantSchedulerRunResultStatus.AlreadyRunning, 0, 0, 0, "Hay otra corrida en curso.");

            var run = new AssistantSchedulerRun { StartedAt = started, Status = AssistantSchedulerRunStatus.Running, CreatedAt = started, UpdatedAt = started };
            await _runs.AddAsync(run);

            var notes = new List<string>();
            var prepared = 0;
            var skipped = 0;
            var failed = 0;
            var ranOutOfTime = false;

            try
            {
                var today = _timeProvider.GetLocalNow().DateTime.Date;
                var previousMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                var userIds = await _usageRepository.GetEnabledUserIdsAsync();

                foreach (var userId in userIds)
                {
                    // Sin tiempo para ni siquiera una pasada más: lo que falte lo toma la corrida siguiente.
                    if (cancellationToken.IsCancellationRequested || UtcNow - started + _options.EstimatedPassDuration > _options.TimeBudget)
                    {
                        ranOutOfTime = true;
                        notes.Add($"Se cortó antes del usuario {userId} (sin tiempo): sigue en la próxima corrida.");
                        break;
                    }

                    try
                    {
                        var status = await _usage.GetStatusAsync(userId);
                        if (status.BudgetReached)
                        {
                            skipped++;
                            notes.Add($"Usuario {userId}: se alcanzó el tope de gasto del mes, no se prepara nada.");
                            continue;
                        }

                        var passes = await _reviews.GetDuePassesAsync(userId);
                        var closePass = passes.FirstOrDefault(p => p.Kind == AssistantReviewKind.MonthClose);
                        if (closePass != null)
                        {
                            var readiness = await _readiness.CheckAsync(userId, previousMonth, today);
                            if (!readiness.ShouldPrepare)
                            {
                                // El cierre espera a que el mes esté completo, y la revisión semanal espera al cierre (tiene prioridad).
                                skipped++;
                                notes.Add($"Usuario {userId}: el cierre de {previousMonth:MM/yyyy} espera datos: {string.Join(" ", readiness.Missing)}");
                                passes = passes.Where(p => p.Kind != AssistantReviewKind.MonthClose && p.Kind != AssistantReviewKind.WeeklyScan).ToList();
                            }
                        }

                        foreach (var pass in passes)
                        {
                            if (UtcNow - started + _options.EstimatedPassDuration > _options.TimeBudget || cancellationToken.IsCancellationRequested)
                            {
                                ranOutOfTime = true;
                                skipped++;
                                notes.Add($"Usuario {userId}: {pass.Kind} queda para la próxima corrida (sin tiempo).");
                                continue;
                            }

                            try
                            {
                                var dto = await _reviews.GenerateAsync(userId, new GenerateAssistantReviewDTO { Kind = pass.Kind }, cancellationToken);
                                if (dto.Status == AssistantReviewStatus.Failed)
                                {
                                    failed++;
                                    notes.Add($"Usuario {userId}: {pass.Kind} falló: {dto.ErrorMessage}");
                                }
                                else
                                {
                                    prepared++;
                                }
                            }
                            catch (BusinessRuleException ex)
                            {
                                // Por ejemplo, otra pasada ya la está generando, o la revisión semanal dejó de tocar.
                                skipped++;
                                notes.Add($"Usuario {userId}: {pass.Kind} no se preparó: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failed++;
                        notes.Add($"Usuario {userId}: error inesperado ({ex.GetType().Name}).");
                        _logger.LogError(ex, "Falló la preparación de madrugada del usuario {UserId}", userId);
                    }
                }

                run.Status = ranOutOfTime
                    ? AssistantSchedulerRunStatus.Partial
                    : failed > 0 && prepared == 0 ? AssistantSchedulerRunStatus.Failed : AssistantSchedulerRunStatus.Completed;
            }
            catch (Exception ex)
            {
                run.Status = AssistantSchedulerRunStatus.Failed;
                failed++;
                notes.Add($"La corrida se cortó: {ex.GetType().Name}.");
                _logger.LogError(ex, "Falló la corrida de preparación de madrugada");
            }

            run.Prepared = prepared;
            run.Skipped = skipped;
            run.Failed = failed;
            run.Notes = Truncate(notes);
            run.FinishedAt = UtcNow;
            run.UpdatedAt = run.FinishedAt.Value;
            await _runs.SaveChangesAsync();

            return new AssistantSchedulerResult(run.Id, run.Status, prepared, skipped, failed, run.Notes);
        }

        private static string? Truncate(List<string> notes)
        {
            if (notes.Count == 0) return null;
            var text = new StringBuilder(string.Join("\n", notes));
            return text.Length <= MaxNotesLength ? text.ToString() : text.ToString(0, MaxNotesLength - 1) + "…";
        }
    }
}
