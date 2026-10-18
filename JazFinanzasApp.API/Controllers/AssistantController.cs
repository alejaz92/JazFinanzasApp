using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.DTO.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    // Devoluciones del asistente (plan-devoluciones-llm.md, D6 y D10). Sin habilitar, todo menos GET status
    // responde el error de negocio "El asistente no está habilitado para tu usuario" (D12).
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AssistantController : ControllerBase
    {
        private readonly IAssistantReviewService _reviewService;
        private readonly IAssistantFindingService _findingService;

        public AssistantController(IAssistantReviewService reviewService, IAssistantFindingService findingService)
        {
            _reviewService = reviewService;
            _findingService = findingService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            return Ok(await _reviewService.GetStatusAsync(GetUserId()));
        }

        [HttpGet("reviews/latest")]
        public async Task<IActionResult> GetLatest()
        {
            return Ok(await _reviewService.GetLatestAsync(GetUserId()));
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetHistory([FromQuery] string? kind)
        {
            return Ok(await _reviewService.GetHistoryAsync(GetUserId(), kind));
        }

        [HttpGet("reviews/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await _reviewService.GetByIdAsync(GetUserId(), id));
        }

        [HttpPost("reviews/generate")]
        public async Task<IActionResult> Generate(GenerateAssistantReviewDTO dto, CancellationToken cancellationToken)
        {
            return Ok(await _reviewService.GenerateAsync(GetUserId(), dto, cancellationToken));
        }

        [HttpPut("reviews/{id:int}/feedback")]
        public async Task<IActionResult> SetFeedback(int id, SetAssistantFeedbackDTO dto)
        {
            return Ok(await _reviewService.SetFeedbackAsync(GetUserId(), id, dto));
        }

        // D8b: los hallazgos activos (y no vencidos), o con all=true también los descartados y vencidos.
        [HttpGet("findings")]
        public async Task<IActionResult> GetFindings([FromQuery] bool all = false)
        {
            return Ok(await _findingService.GetAsync(GetUserId(), all));
        }

        // Descarta o califica un hallazgo, sin entrar a la pantalla del asistente.
        [HttpPut("findings/{id:int}")]
        public async Task<IActionResult> UpdateFinding(int id, UpdateAssistantFindingDTO dto)
        {
            return Ok(await _findingService.UpdateAsync(GetUserId(), id, dto));
        }

        // D10: regenera con otra versión de instrucciones sobre el mismo contexto guardado. Solo Admin.
        [Authorize(Roles = "Admin")]
        [HttpPost("reviews/{id:int}/preview")]
        public async Task<IActionResult> Preview(int id, [FromQuery] string promptVersion, CancellationToken cancellationToken)
        {
            return Ok(await _reviewService.PreviewAsync(GetUserId(), id, promptVersion, cancellationToken));
        }
    }
}
