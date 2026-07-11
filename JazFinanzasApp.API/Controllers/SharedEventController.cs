using JazFinanzasApp.API.Business.DTO.SharedEvent;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/shared-event")]
    [ApiController]
    public class SharedEventController : ControllerBase
    {
        private readonly ISharedEventService _sharedEventService;

        public SharedEventController(ISharedEventService sharedEventService)
        {
            _sharedEventService = sharedEventService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeClosed = false)
        {
            var result = await _sharedEventService.GetAllForUserAsync(GetUserId(), includeClosed);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sharedEventService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SharedEventAddDTO dto)
        {
            var created = await _sharedEventService.CreateAsync(GetUserId(), dto);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SharedEventEditDTO dto)
        {
            await _sharedEventService.UpdateAsync(GetUserId(), id, dto);
            return Ok();
        }

        [HttpPost("{id}/participants")]
        public async Task<IActionResult> AddParticipant(int id, SharedEventParticipantAddDTO dto)
        {
            var result = await _sharedEventService.AddParticipantAsync(GetUserId(), id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}/participants/{personId}")]
        public async Task<IActionResult> RemoveParticipant(int id, int personId)
        {
            await _sharedEventService.RemoveParticipantAsync(GetUserId(), id, personId);
            return Ok();
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(int id)
        {
            await _sharedEventService.CloseAsync(GetUserId(), id);
            return Ok();
        }

        [HttpPost("{id}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            await _sharedEventService.ReopenAsync(GetUserId(), id);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _sharedEventService.DeleteAsync(GetUserId(), id);
            return Ok();
        }
    }
}
