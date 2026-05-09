using JazFinanzasApp.API.Business.DTO.Portfolio;
using JazFinanzasApp.API.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _portfolioService.GetAllForUserAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _portfolioService.GetByIdAsync(GetUserId(), id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PortfolioDTO portfolioDTO)
        {
            await _portfolioService.CreatePortfolioAsync(GetUserId(), portfolioDTO);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PortfolioDTO portfolioDTO)
        {
            await _portfolioService.UpdatePortfolioAsync(GetUserId(), id, portfolioDTO);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _portfolioService.DeletePortfolioAsync(GetUserId(), id);
            return Ok();
        }

        [HttpGet("default")]
        public async Task<IActionResult> GetDefault()
        {
            var result = await _portfolioService.GetDefaultPortfolioAsync(GetUserId());
            return Ok(result);
        }
    }
}
