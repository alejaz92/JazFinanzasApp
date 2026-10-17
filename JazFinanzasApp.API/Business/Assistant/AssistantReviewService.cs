using System.Text.Json;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JazFinanzasApp.API.Business.Assistant
{
    public interface IAssistantReviewService
    {
        Task<AssistantStatusDTO> GetStatusAsync(int userId);
        Task<AssistantLatestDTO> GetLatestAsync(int userId);
        Task<List<AssistantReviewSummaryDTO>> GetHistoryAsync(int userId, string? kind);
        Task<AssistantReviewDTO> GetByIdAsync(int userId, int id);
        Task<AssistantReviewDTO> GenerateAsync(int userId, GenerateAssistantReviewDTO request, CancellationToken cancellationToken = default);
        Task<AssistantReviewDTO> PreviewAsync(int userId, int reviewId, string promptVersion, CancellationToken cancellationToken = default);
        Task<AssistantFeedbackDTO> SetFeedbackAsync(int userId, int reviewId, SetAssistantFeedbackDTO request);
    }

    // Devoluciones del asistente: decide cuál falta (D6), genera, guarda, controla las cifras (D7), regenera
    // conservando la anterior y compara versiones de instrucciones (D10). Los números los calculan las fuentes
    // de contexto; acá solo se arma el pedido, se llama al modelo a través de AssistantUsageService (que lleva
    // el tope y el registro de gasto) y se guarda el resultado.
    public class AssistantReviewService : IAssistantReviewService
    {
        // Día del mes a partir del cual falta la devolución de mitad de mes (D6, paso 2).
        public const int MidMonthFromDay = 15;

        // Una pasada Pending más vieja que esto se dio por interrumpida: el límite del pedido del App Service es de 230 s.
        public static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(5);

        // Una pasada fallida no se reintenta sola hasta pasado este tiempo, para que cada entrada a la app no
        // vuelva a gastar (una negativa o un corte se pagan) mientras algo sigue fallando.
        public static readonly TimeSpan FailedCooldown = TimeSpan.FromHours(1);

        private const int MaxHeadlineLength = 300;
        private const int MaxErrorLength = 1000;
        private const int MaxHighlights = 3;
        private const int MaxCommentLength = 1000;

        private static readonly JsonSerializerOptions ModelJson = new() { PropertyNameCaseInsensitive = true };

        private readonly IAssistantReviewRepository _repository;
        private readonly IAssistantUsageService _usage;
        private readonly AssistantContextBuilder _contextBuilder;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AssistantReviewService> _logger;

        public AssistantReviewService(
            IAssistantReviewRepository repository,
            IAssistantUsageService usage,
            AssistantContextBuilder contextBuilder,
            TimeProvider timeProvider,
            ILogger<AssistantReviewService> logger)
        {
            _repository = repository;
            _usage = usage;
            _contextBuilder = contextBuilder;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        private DateTime Today => _timeProvider.GetLocalNow().DateTime.Date;
        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
        private static DateTime FirstOfMonth(DateTime d) => new(d.Year, d.Month, 1);

        // --- lecturas ---

        public async Task<AssistantStatusDTO> GetStatusAsync(int userId)
        {
            var status = await _usage.GetStatusAsync(userId);
            return new AssistantStatusDTO
            {
                Enabled = status.Enabled,
                MonthlyBudgetUsd = status.MonthlyBudgetUsd,
                SpentUsd = status.SpentUsd,
                RenewsOn = status.RenewsOn,
                BudgetReached = status.BudgetReached
            };
        }

        // D6: primero el cierre del mes anterior, después (desde el día 15) la mitad del mes en curso, y si no
        // falta ninguna, la última devolución.
        public async Task<AssistantLatestDTO> GetLatestAsync(int userId)
        {
            await _usage.EnsureEnabledAsync(userId);

            var currentMonth = FirstOfMonth(Today);
            var previousMonth = currentMonth.AddMonths(-1);

            if (!await IsCoveredAsync(userId, AssistantReviewKind.MonthClose, previousMonth))
                return NotGenerated(AssistantReviewKind.MonthClose, previousMonth);

            if (Today.Day >= MidMonthFromDay && !await IsCoveredAsync(userId, AssistantReviewKind.MidMonth, currentMonth))
                return NotGenerated(AssistantReviewKind.MidMonth, currentMonth);

            var latest = await _repository.GetLatestReadyAsync(userId);
            if (latest == null)
                return new AssistantLatestDTO { State = AssistantLatestState.Empty };

            return new AssistantLatestDTO { State = AssistantLatestState.Ready, Review = await ToDtoAsync(latest) };
        }

        public async Task<List<AssistantReviewSummaryDTO>> GetHistoryAsync(int userId, string? kind)
        {
            await _usage.EnsureEnabledAsync(userId);
            if (!string.IsNullOrEmpty(kind) && kind != AssistantReviewKind.MonthClose && kind != AssistantReviewKind.MidMonth)
                throw new BusinessRuleException("El tipo de devolución no es válido.");

            var reviews = await _repository.GetCurrentUserReviewsAsync(userId, kind);
            var feedback = await _repository.GetFeedbackByReviewIdsAsync(reviews.Select(r => r.Id));
            return reviews.Select(r => new AssistantReviewSummaryDTO
            {
                Id = r.Id,
                Kind = r.Kind,
                PeriodStart = r.PeriodStart,
                CutoffDate = r.CutoffDate,
                Status = r.Status,
                Headline = r.Headline,
                HasUnverifiedFigures = !string.IsNullOrEmpty(r.UnverifiedFiguresJson),
                Rating = feedback.TryGetValue(r.Id, out var f) ? f.Rating : null,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<AssistantReviewDTO> GetByIdAsync(int userId, int id)
        {
            await _usage.EnsureEnabledAsync(userId);
            var review = await _repository.GetByIdAsync(userId, id)
                ?? throw new NotFoundException("No se encontró la devolución.");
            return await ToDtoAsync(review);
        }

        // --- generación ---

        public async Task<AssistantReviewDTO> GenerateAsync(int userId, GenerateAssistantReviewDTO request, CancellationToken cancellationToken = default)
        {
            await _usage.EnsureEnabledAsync(userId);
            var (periodStart, cutoff) = ResolvePeriod(request.Kind, request.PeriodStart);

            var existing = await GetCoveringReviewAsync(userId, request.Kind, periodStart);
            if (existing != null && existing.Status == AssistantReviewStatus.Pending)
                throw new BusinessRuleException("Ya se está generando esta devolución. Esperá unos segundos y volvé a abrirla.");
            if (existing != null && !request.Regenerate)
                return await ToDtoAsync(existing);

            await ExpireStalePendingAsync(userId, request.Kind, periodStart, AssistantReviewPurpose.User);

            // Un período sin movimientos no llama al modelo y no vuelve a intentarse (D6).
            if (!await _repository.HasActivityAsync(userId, periodStart, cutoff))
            {
                var empty = NewReview(userId, request.Kind, periodStart, cutoff, AssistantReviewPurpose.User, AssistantReviewStatus.NoActivity);
                await _repository.AddAsync(empty);
                await MakeCurrentAsync(empty);
                return await ToDtoAsync(empty);
            }

            var review = NewReview(userId, request.Kind, periodStart, cutoff, AssistantReviewPurpose.User, AssistantReviewStatus.Pending);
            await AddPendingAsync(review);

            string contextJson;
            try
            {
                var context = await _contextBuilder.BuildAsync(new AssistantContextRequest(userId, request.Kind, periodStart, cutoff));
                contextJson = AssistantContextBuilder.Serialize(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo armar el contexto del asistente para el usuario {UserId}, {Kind} {Period}", userId, request.Kind, periodStart);
                Fail(review, "No se pudieron reunir los datos para la devolución. Probá de nuevo más tarde.");
                await FinishAsync(review);
                return await ToDtoAsync(review);
            }

            review.ContextJson = contextJson;
            var promptVersion = AssistantPrompts.CurrentVersion(request.Kind);
            await ExecuteAsync(review, contextJson, promptVersion, AssistantApiCallPurpose.Review, cancellationToken);
            return await ToDtoAsync(review);
        }

        // D10: genera con otra versión de instrucciones sobre el MISMO contexto guardado. Queda como Eval, sin
        // reemplazar la devolución vigente, y su llamada no cuenta para el tope. El rol Admin lo exige el controlador.
        public async Task<AssistantReviewDTO> PreviewAsync(int userId, int reviewId, string promptVersion, CancellationToken cancellationToken = default)
        {
            await _usage.EnsureEnabledAsync(userId);
            var source = await _repository.GetByIdAsync(userId, reviewId)
                ?? throw new NotFoundException("No se encontró la devolución.");
            if (source.Kind == AssistantReviewKind.WeeklyScan || string.IsNullOrEmpty(source.ContextJson))
                throw new BusinessRuleException("Esa devolución no tiene los datos guardados para compararla.");

            var version = AssistantPrompts.ResolveVersion(source.Kind, promptVersion);

            var existing = await GetCoveringReviewAsync(userId, source.Kind, source.PeriodStart, AssistantReviewPurpose.Eval);
            if (existing != null && existing.Status == AssistantReviewStatus.Pending)
                throw new BusinessRuleException("Ya se está generando una comparación para este período.");
            await ExpireStalePendingAsync(userId, source.Kind, source.PeriodStart, AssistantReviewPurpose.Eval);

            var review = NewReview(userId, source.Kind, source.PeriodStart, source.CutoffDate, AssistantReviewPurpose.Eval, AssistantReviewStatus.Pending);
            review.ContextJson = source.ContextJson;
            await AddPendingAsync(review);

            await ExecuteAsync(review, source.ContextJson, version, AssistantApiCallPurpose.Eval, cancellationToken);
            return await ToDtoAsync(review);
        }

        // Llama al modelo y deja la pasada en Ready o Failed. Los errores de negocio (tope, falla del servicio) no se
        // propagan: quedan como Failed con su mensaje, que es lo que ve el usuario.
        private async Task ExecuteAsync(AssistantReview review, string contextJson, string promptVersion, string callPurpose, CancellationToken cancellationToken)
        {
            review.PromptVersion = promptVersion;
            try
            {
                var systemPrompt = AssistantPrompts.Load(promptVersion);
                var request = new AssistantModelRequest(systemPrompt, contextJson, AssistantPrompts.ReviewOutputSchema, AssistantEffort.High);
                var response = await _usage.CallModelAsync(
                    new AssistantModelCall(review.UserId, callPurpose, promptVersion, review.Id, request), cancellationToken);

                review.Model = response.Model;
                Interpret(review, response, contextJson);
            }
            catch (OperationCanceledException)
            {
                Fail(review, "La generación se canceló antes de terminar.");
                await FinishAsync(review);
                throw;
            }
            catch (BusinessRuleException ex)
            {
                Fail(review, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la generación del asistente (revisión {ReviewId})", review.Id);
                Fail(review, AssistantModelException.DefaultMessage);
            }

            await FinishAsync(review);
        }

        // Una negativa, un corte o una respuesta que no tiene el formato esperado dejan la devolución en Failed.
        private void Interpret(AssistantReview review, AssistantModelResponse response, string contextJson)
        {
            if (response.IsRefusal)
            {
                Fail(review, "El modelo no pudo responder a este pedido. Probá de nuevo más tarde.");
                return;
            }
            if (response.IsTruncated)
            {
                Fail(review, "La respuesta del asistente se cortó por exceder el largo máximo.");
                return;
            }
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                Fail(review, "El asistente no devolvió ningún texto.");
                return;
            }

            ModelReviewOutput? output;
            try
            {
                output = JsonSerializer.Deserialize<ModelReviewOutput>(response.Text, ModelJson);
            }
            catch (JsonException)
            {
                output = null;
            }

            var highlights = output?.Highlights?.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).Take(MaxHighlights).ToList();
            if (output == null || string.IsNullOrWhiteSpace(output.Headline) || string.IsNullOrWhiteSpace(output.Body) || highlights == null || highlights.Count == 0)
            {
                Fail(review, "La respuesta del asistente no tuvo el formato esperado.");
                return;
            }

            var headline = output.Headline.Trim();
            review.Headline = headline.Length > MaxHeadlineLength ? headline[..MaxHeadlineLength] : headline;
            review.HighlightsJson = JsonSerializer.Serialize(highlights);
            review.Body = output.Body.Trim();

            // Control de cifras (D7): no bloquea, solo marca.
            var text = string.Join("\n", new[] { review.Headline }.Concat(highlights).Append(review.Body));
            var unverified = AssistantFigureChecker.FindUnverified(text, contextJson);
            review.UnverifiedFiguresJson = unverified.Count > 0 ? JsonSerializer.Serialize(unverified) : null;

            review.Status = AssistantReviewStatus.Ready;
            review.ErrorMessage = null;
        }

        private static void Fail(AssistantReview review, string message)
        {
            review.Status = AssistantReviewStatus.Failed;
            review.ErrorMessage = message.Length > MaxErrorLength ? message[..MaxErrorLength] : message;
        }

        // Guarda el resultado. Una devolución de usuario pasa a ser la vigente (la anterior queda, sin IsCurrent);
        // una fallida solo lo es si no hay otra buena del mismo período, para no taparla. Eval nunca es la vigente.
        private async Task FinishAsync(AssistantReview review)
        {
            review.UpdatedAt = UtcNow;
            if (review.Purpose == AssistantReviewPurpose.User)
            {
                if (review.Status == AssistantReviewStatus.Ready)
                {
                    await MakeCurrentAsync(review);
                    return;
                }

                var siblings = await _repository.GetForPeriodAsync(review.UserId, review.Kind, review.PeriodStart, AssistantReviewPurpose.User);
                var hasGoodCurrent = siblings.Any(s => s.Id != review.Id && s.IsCurrent
                    && (s.Status == AssistantReviewStatus.Ready || s.Status == AssistantReviewStatus.NoActivity));
                if (!hasGoodCurrent)
                {
                    await MakeCurrentAsync(review);
                    return;
                }
            }
            await _repository.SaveChangesAsync();
        }

        private async Task MakeCurrentAsync(AssistantReview review)
        {
            var siblings = await _repository.GetForPeriodAsync(review.UserId, review.Kind, review.PeriodStart, AssistantReviewPurpose.User);
            foreach (var sibling in siblings.Where(s => s.Id != review.Id && s.IsCurrent))
            {
                sibling.IsCurrent = false;
                sibling.UpdatedAt = UtcNow;
            }
            review.IsCurrent = true;
            review.UpdatedAt = UtcNow;
            await _repository.SaveChangesAsync();
        }

        // Crea la fila Pending. El índice único filtrado de la base impide dos Pending del mismo período aunque dos
        // pedidos pasen a la vez el chequeo previo: el segundo recibe el mismo error que si lo hubiera visto antes.
        private async Task AddPendingAsync(AssistantReview review)
        {
            try
            {
                await _repository.AddAsync(review);
            }
            catch (DbUpdateException)
            {
                throw new BusinessRuleException("Ya se está generando esta devolución. Esperá unos segundos y volvé a abrirla.");
            }
        }

        // --- feedback ---

        public async Task<AssistantFeedbackDTO> SetFeedbackAsync(int userId, int reviewId, SetAssistantFeedbackDTO request)
        {
            await _usage.EnsureEnabledAsync(userId);
            if (request.Rating != 1 && request.Rating != -1)
                throw new BusinessRuleException("La calificación tiene que ser útil o no útil.");
            var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
            if (comment != null && comment.Length > MaxCommentLength)
                throw new BusinessRuleException($"El comentario no puede superar los {MaxCommentLength} caracteres.");

            var review = await _repository.GetByIdAsync(userId, reviewId)
                ?? throw new NotFoundException("No se encontró la devolución.");
            if (review.Status != AssistantReviewStatus.Ready)
                throw new BusinessRuleException("Solo se puede calificar una devolución terminada.");

            // Calificar de nuevo reemplaza la calificación anterior.
            var existing = (await _repository.GetFeedbackByReviewIdsAsync(new[] { review.Id })).GetValueOrDefault(review.Id);
            if (existing == null)
            {
                existing = new AssistantFeedback { ReviewId = review.Id };
                await _repository.AddFeedbackAsync(existing);
            }
            existing.Rating = request.Rating;
            existing.HasWrongFigures = request.HasWrongFigures;
            existing.Comment = comment;
            existing.UpdatedAt = UtcNow;
            await _repository.SaveChangesAsync();

            return new AssistantFeedbackDTO { Rating = existing.Rating, HasWrongFigures = existing.HasWrongFigures, Comment = existing.Comment };
        }

        // --- ayudas ---

        private (DateTime PeriodStart, DateTime Cutoff) ResolvePeriod(string kind, DateTime? requested)
        {
            var currentMonth = FirstOfMonth(Today);
            switch (kind)
            {
                case AssistantReviewKind.MonthClose:
                {
                    var start = requested.HasValue ? FirstOfMonth(requested.Value) : currentMonth.AddMonths(-1);
                    if (start >= currentMonth)
                        throw new BusinessRuleException("El cierre de mes solo se puede generar para un mes que ya terminó.");
                    return (start, start.AddMonths(1).AddDays(-1));
                }
                case AssistantReviewKind.MidMonth:
                {
                    var start = requested.HasValue ? FirstOfMonth(requested.Value) : currentMonth;
                    if (start != currentMonth)
                        throw new BusinessRuleException("La devolución de mitad de mes es solo para el mes en curso.");
                    if (Today.Day < 2)
                        throw new BusinessRuleException("Todavía no hay datos del mes en curso para analizar.");
                    return (start, Today.AddDays(-1));
                }
                case AssistantReviewKind.WeeklyScan:
                    throw new BusinessRuleException("La revisión semanal todavía no está disponible.");
                default:
                    throw new BusinessRuleException("El tipo de devolución no es válido.");
            }
        }

        // La pasada que ya cubre el período, si hay una: terminada (con texto o sin movimientos), en curso, o
        // fallida hace menos del tiempo de espera (para no reintentar sola en cada entrada).
        private async Task<AssistantReview?> GetCoveringReviewAsync(int userId, string kind, DateTime periodStart, string purpose = AssistantReviewPurpose.User)
        {
            var rows = await _repository.GetForPeriodAsync(userId, kind, periodStart, purpose);
            var now = UtcNow;
            return rows.FirstOrDefault(r =>
                r.Status == AssistantReviewStatus.Ready
                || r.Status == AssistantReviewStatus.NoActivity
                || (r.Status == AssistantReviewStatus.Pending && now - r.CreatedAt < PendingTimeout)
                || (r.Status == AssistantReviewStatus.Failed && now - r.CreatedAt < FailedCooldown));
        }

        private async Task<bool> IsCoveredAsync(int userId, string kind, DateTime periodStart) =>
            await GetCoveringReviewAsync(userId, kind, periodStart) != null;

        // Una Pending vieja es de un proceso que se cayó: se la da por fallida para liberar el período.
        private async Task ExpireStalePendingAsync(int userId, string kind, DateTime periodStart, string purpose)
        {
            var rows = await _repository.GetForPeriodAsync(userId, kind, periodStart, purpose);
            var stale = rows.Where(r => r.Status == AssistantReviewStatus.Pending && UtcNow - r.CreatedAt >= PendingTimeout).ToList();
            if (stale.Count == 0) return;
            foreach (var review in stale)
            {
                Fail(review, "La generación anterior se interrumpió.");
                review.UpdatedAt = UtcNow;
            }
            await _repository.SaveChangesAsync();
        }

        private AssistantReview NewReview(int userId, string kind, DateTime periodStart, DateTime cutoff, string purpose, string status) => new()
        {
            UserId = userId,
            Kind = kind,
            PeriodStart = periodStart,
            CutoffDate = cutoff,
            Purpose = purpose,
            Status = status,
            IsCurrent = false,
            CreatedAt = UtcNow,
            UpdatedAt = UtcNow
        };

        private static AssistantLatestDTO NotGenerated(string kind, DateTime periodStart) => new()
        {
            State = AssistantLatestState.NotGenerated,
            MissingKind = kind,
            MissingPeriodStart = periodStart
        };

        private async Task<AssistantReviewDTO> ToDtoAsync(AssistantReview review)
        {
            var feedback = (await _repository.GetFeedbackByReviewIdsAsync(new[] { review.Id })).GetValueOrDefault(review.Id);
            return new AssistantReviewDTO
            {
                Id = review.Id,
                Kind = review.Kind,
                PeriodStart = review.PeriodStart,
                CutoffDate = review.CutoffDate,
                Status = review.Status,
                IsCurrent = review.IsCurrent,
                Purpose = review.Purpose,
                Headline = review.Headline,
                Highlights = ReadList(review.HighlightsJson),
                Body = review.Body,
                UnverifiedFigures = ReadList(review.UnverifiedFiguresJson),
                ErrorMessage = review.ErrorMessage,
                PromptVersion = review.PromptVersion,
                Model = review.Model,
                CreatedAt = review.CreatedAt,
                Feedback = feedback == null ? null : new AssistantFeedbackDTO
                {
                    Rating = feedback.Rating,
                    HasWrongFigures = feedback.HasWrongFigures,
                    Comment = feedback.Comment
                }
            };
        }

        private static List<string> ReadList(string? json) =>
            string.IsNullOrEmpty(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

        private record ModelReviewOutput(string? Headline, List<string>? Highlights, string? Body);
    }
}
