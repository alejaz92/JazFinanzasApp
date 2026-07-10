using JazFinanzasApp.API.Business.DTO.Trip;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tripService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _tripService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TripAddDTO dto)
        {
            var created = await _tripService.CreateTripAsync(GetUserId(), dto);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TripEditDTO dto)
        {
            await _tripService.UpdateTripAsync(GetUserId(), id, dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tripService.DeleteTripAsync(GetUserId(), id);
            return Ok();
        }
    }
}
