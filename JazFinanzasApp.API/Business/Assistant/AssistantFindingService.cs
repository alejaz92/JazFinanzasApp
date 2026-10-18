using System.Globalization;
using System.Text;
using System.Text.Json;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;

namespace JazFinanzasApp.API.Business.Assistant
{
    // Un hallazgo tal como lo devuelve el modelo (D4): lo que el esquema de salida garantiza, sin validar.
    public record AssistantModelFinding(string? Title, string? Detail, string? Category, string? Kind);

    // Qué pasó con los hallazgos de una pasada: cuántos quedaron activos, cuántos se descartaron por repetidos
    // (D15) y cuántos sobraron del tope de tres.
    public record AssistantFindingSaveResult(int Saved, int Repeated, int OverCap);

    public interface IAssistantFindingService
    {
        // Guarda los hallazgos de una pasada terminada: aplica el descarte por Fingerprint, el tope de tres, el
        // vencimiento y el control de cifras.
        Task<AssistantFindingSaveResult> SaveFromReviewAsync(AssistantReview review, IReadOnlyList<AssistantModelFinding> findings, string contextJson);

        // Los hallazgos activos (y no vencidos) o, con includeAll, también los descartados y vencidos.
        Task<List<AssistantFindingDTO>> GetAsync(int userId, bool includeAll);

        // Descarta o califica un hallazgo.
        Task<AssistantFindingDTO> UpdateAsync(int userId, int id, UpdateAssistantFindingDTO request);
    }

    // Hallazgos del asistente: lo que las reglas fijas no prevén y el modelo señala, en cada devolución y en la
    // revisión semanal (plan-devoluciones-llm.md, Flujo 2, D8b y D15).
    public class AssistantFindingService : IAssistantFindingService
    {
        // Como mucho tres hallazgos nuevos por pasada (D15, resguardo 3).
        public const int MaxFindingsPerReview = 3;

        // Cuántos hallazgos del modelo se miran como máximo, para acotar el trabajo si devolviera muchos.
        private const int MaxFindingsConsidered = 10;

        // Un hallazgo vence a los 14 días o a fin de mes, lo que llegue primero (D8b).
        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

        private const int MaxTitleLength = 200;
        private const int MaxDetailLength = 1000;
        private const string NoCategory = "general";

        private readonly IAssistantFindingRepository _repository;
        private readonly ITransactionClassRepository _classRepository;
        private readonly IAssistantUsageService _usage;
        private readonly TimeProvider _timeProvider;

        public AssistantFindingService(
            IAssistantFindingRepository repository,
            ITransactionClassRepository classRepository,
            IAssistantUsageService usage,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _classRepository = classRepository;
            _usage = usage;
            _timeProvider = timeProvider;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
        private DateTime Today => _timeProvider.GetLocalNow().DateTime.Date;

        public async Task<AssistantFindingSaveResult> SaveFromReviewAsync(AssistantReview review, IReadOnlyList<AssistantModelFinding> findings, string contextJson)
        {
            var valid = findings
                .Where(f => !string.IsNullOrWhiteSpace(f.Title) && !string.IsNullOrWhiteSpace(f.Detail))
                .Take(MaxFindingsConsidered)
                .ToList();
            if (valid.Count == 0)
                return new AssistantFindingSaveResult(0, 0, 0);

            var now = UtcNow;
            var monthStart = new DateTime(Today.Year, Today.Month, 1);
            var expiresAt = Min(now + Lifetime, monthStart.AddMonths(1));

            var seen = await _repository.GetFingerprintsSinceAsync(review.UserId, monthStart);
            var classes = valid.Any(f => !string.IsNullOrWhiteSpace(f.Category))
                ? (await _classRepository.GetByUserIdAsync(review.UserId)).ToList()
                : new List<TransactionClass>();

            var rows = new List<AssistantFinding>();
            int saved = 0, repeated = 0, overCap = 0;
            foreach (var finding in valid)
            {
                var fingerprint = BuildFingerprint(finding.Category, finding.Kind);
                var isRepeat = !seen.Add(fingerprint);
                if (!isRepeat && saved >= MaxFindingsPerReview)
                {
                    overCap++;
                    continue;
                }

                var title = Truncate(finding.Title!.Trim(), MaxTitleLength);
                var detail = Truncate(finding.Detail!.Trim(), MaxDetailLength);
                var unverified = AssistantFigureChecker.FindUnverified($"{title}\n{detail}", contextJson);

                rows.Add(new AssistantFinding
                {
                    ReviewId = review.Id,
                    UserId = review.UserId,
                    Title = title,
                    Detail = detail,
                    TransactionClassId = isRepeat ? null : ResolveClass(classes, finding.Category),
                    Fingerprint = fingerprint,
                    UnverifiedFiguresJson = unverified.Count > 0 ? JsonSerializer.Serialize(unverified) : null,
                    Status = isRepeat ? AssistantFindingStatus.Repeated : AssistantFindingStatus.Active,
                    ExpiresAt = isRepeat ? now : expiresAt,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                if (isRepeat) repeated++; else saved++;
            }

            await _repository.AddRangeAsync(rows);
            await _repository.SaveChangesAsync();
            return new AssistantFindingSaveResult(saved, repeated, overCap);
        }

        public async Task<List<AssistantFindingDTO>> GetAsync(int userId, bool includeAll)
        {
            await _usage.EnsureEnabledAsync(userId);
            var findings = await _repository.GetByUserAsync(userId, onlyActive: !includeAll);
            if (ExpireDue(findings))
                await _repository.SaveChangesAsync();

            return findings
                .Where(f => includeAll || f.Status == AssistantFindingStatus.Active)
                .Select(ToDto)
                .ToList();
        }

        public async Task<AssistantFindingDTO> UpdateAsync(int userId, int id, UpdateAssistantFindingDTO request)
        {
            await _usage.EnsureEnabledAsync(userId);
            if (request.Dismiss != true && request.Rating == null)
                throw new BusinessRuleException("Falta indicar si se descarta o la calificación.");
            if (request.Rating != null && request.Rating != 1 && request.Rating != -1)
                throw new BusinessRuleException("La calificación tiene que ser útil o no útil.");

            var finding = await _repository.GetByIdAsync(userId, id)
                ?? throw new NotFoundException("No se encontró el hallazgo.");
            ExpireDue(new[] { finding });

            if (request.Dismiss == true)
            {
                if (finding.Status == AssistantFindingStatus.Expired)
                    throw new BusinessRuleException("El hallazgo ya venció.");
                finding.Status = AssistantFindingStatus.Dismissed;
            }
            if (request.Rating != null)
                finding.Rating = request.Rating;

            finding.UpdatedAt = UtcNow;
            await _repository.SaveChangesAsync();
            return ToDto(finding);
        }

        // Un hallazgo activo vencido pasa a Expired al leerse: no hace falta ninguna tarea programada (D8b).
        private bool ExpireDue(IEnumerable<AssistantFinding> findings)
        {
            var now = UtcNow;
            var changed = false;
            foreach (var finding in findings.Where(f => f.Status == AssistantFindingStatus.Active && f.ExpiresAt <= now))
            {
                finding.Status = AssistantFindingStatus.Expired;
                finding.UpdatedAt = now;
                changed = true;
            }
            return changed;
        }

        // Categoría + tipo, normalizados (D15): sin tildes, en minúsculas y con los espacios colapsados.
        public static string BuildFingerprint(string? category, string? kind)
        {
            var cat = Normalize(category);
            var k = Normalize(kind);
            return $"{(cat.Length == 0 ? NoCategory : cat)}|{(k.Length == 0 ? "other" : k)}";
        }

        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            var plain = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            return string.Join(' ', plain.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        // La categoría del hallazgo por nombre. Si hay más de una con ese nombre no se adivina: sin link.
        private static int? ResolveClass(IReadOnlyList<TransactionClass> classes, string? category)
        {
            var wanted = Normalize(category);
            if (wanted.Length == 0) return null;
            var matches = classes.Where(c => Normalize(c.Description) == wanted).ToList();
            return matches.Count == 1 ? matches[0].Id : null;
        }

        private static string Truncate(string text, int max) => text.Length > max ? text[..max] : text;
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;

        private static AssistantFindingDTO ToDto(AssistantFinding f) => new()
        {
            Id = f.Id,
            ReviewId = f.ReviewId,
            Title = f.Title,
            Detail = f.Detail,
            TransactionClassId = f.TransactionClassId,
            Status = f.Status,
            ExpiresAt = f.ExpiresAt,
            Rating = f.Rating,
            UnverifiedFigures = string.IsNullOrEmpty(f.UnverifiedFiguresJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(f.UnverifiedFiguresJson) ?? new List<string>(),
            CreatedAt = f.CreatedAt
        };
    }
}
