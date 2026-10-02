using JazFinanzasApp.API.Business.DTO.BondCollection;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BondCollectionController : ControllerBase
    {
        private readonly IBondCollectionService _bondCollectionService;

        public BondCollectionController(IBondCollectionService bondCollectionService)
        {
            _bondCollectionService = bondCollectionService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _bondCollectionService.GetPendingAsync(GetUserId());
            return Ok(pending);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistered()
        {
            var registered = await _bondCollectionService.GetRegisteredAsync(GetUserId());
            return Ok(registered);
        }

        [HttpGet("last-interest-class")]
        public async Task<IActionResult> GetLastInterestClass()
        {
            var classId = await _bondCollectionService.GetLastInterestClassIdAsync(GetUserId());
            return Ok(classId);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(BondCollectionRegisterDTO dto)
        {
            var id = await _bondCollectionService.RegisterAsync(GetUserId(), dto);
            return Ok(id);
        }

        [HttpPost("dismiss")]
        public async Task<IActionResult> Dismiss(BondCollectionDismissDTO dto)
        {
            await _bondCollectionService.DismissAsync(GetUserId(), dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _bondCollectionService.DeleteAsync(GetUserId(), id);
            return Ok();
        }
    }
}
