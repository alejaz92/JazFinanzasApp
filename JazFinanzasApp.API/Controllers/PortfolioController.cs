﻿using JazFinanzasApp.API.Business.DTO.Portfolio;
using JazFinanzasApp.API.Infrastructure.Domain;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioRepository _portfolioRepository;
        public PortfolioController(IPortfolioRepository portfolioRepository)
        {
            _portfolioRepository = portfolioRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
            // convert to DTO
            var portfoliosDTO = portfolios.Select(p => new PortfolioDTO
            {
                Id = p.Id,
                Name = p.Name,
                IsDefault = p.IsDefault
            }).ToList();
            return Ok(portfoliosDTO);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var portfolio = await _portfolioRepository.GetByIdAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }
            if (portfolio.UserId != userId)
            {
                return Unauthorized();
            }
            // convert to DTO
            var portfolioDTO = new PortfolioDTO
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                IsDefault = portfolio.IsDefault
            };
            return Ok(portfolioDTO);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PortfolioDTO portfolioDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);

            // Check if portfolio name already exists for the user
            var existingPortfolio = await _portfolioRepository.FindAsync(p => p.Name == portfolioDTO.Name && p.UserId == userId);
            if (existingPortfolio.Any())
            {
                return BadRequest("Portfolio with this name already exists.");
            }

            // Check if user has a default portfolio
            if (portfolioDTO.IsDefault)
            {
                var defaultPortfolio = await _portfolioRepository.GetDefaultPortfolio(userId);
                if (defaultPortfolio != null)
                {
                    return BadRequest("User already has a default portfolio.");
                }
            }

            var portfolio = new Portfolio
            {
                Name = portfolioDTO.Name,
                UserId = userId,
                IsDefault = portfolioDTO.IsDefault
            };

            await _portfolioRepository.AddAsync(portfolio);

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PortfolioDTO portfolioDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var portfolio = await _portfolioRepository.GetByIdAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }
            if (portfolio.UserId != userId)
            {
                return Unauthorized();
            }
            // Check if portfolio name already exists for the user
            var existingPortfolio = await _portfolioRepository.FindAsync(p => p.Name == portfolioDTO.Name && p.UserId == userId && p.Id != id);
            if (existingPortfolio.Any())
            {
                return BadRequest("Portfolio with this name already exists.");
            }
            portfolio.Name = portfolioDTO.Name;
            await _portfolioRepository.UpdateAsync(portfolio);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var portfolio = await _portfolioRepository.GetByIdAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }
            if (portfolio.UserId != userId)
            {
                return Unauthorized();
            }

            // Check if portfolio is default
            if (portfolio.IsDefault) return BadRequest("Default portfolio cannot be deleted.");

            // Check if portfolio is used in transactions
            if (await _portfolioRepository.IsPortfolioUsedInTransactions(id))
            {
                return BadRequest("Portfolio is used in transactions and cannot be deleted.");
            }
            await _portfolioRepository.DeleteAsync(portfolio.Id);
            return Ok();
        }

        // get default portfolio
        [HttpGet("default")]
        public async Task<IActionResult> GetDefaultPortfolio()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var portfolio = await _portfolioRepository.GetDefaultPortfolio(userId);
            if (portfolio == null)
            {
                return NotFound();
            }
            // convert to DTO
            var portfolioDTO = new PortfolioDTO
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                IsDefault = portfolio.IsDefault
            };
            return Ok(portfolioDTO);
        }


    }
}
