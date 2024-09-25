using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.movementClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{


    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MovementClassController : ControllerBase
    {
        private readonly IGenericRepository<MovementClass> _movementClassRepository;
        public MovementClassController(IGenericRepository<MovementClass> movementClassRepository)
        {
            _movementClassRepository = movementClassRepository;
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

            var movementClasses = await _movementClassRepository.GetByUserIdAsync(userId);

            // convert to DTO

            var movementClassesDTO = movementClasses.Select(mc => new MovementClassListDTO
            {
                Description = mc.Description,
                IncExp = mc.IncExp
            }).ToList();
            return Ok(movementClassesDTO);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMovementClass(MovementClassListDTO movementClassDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var movementClass = new MovementClass
            {
                Description = movementClassDTO.Description,
                IncExp = movementClassDTO.IncExp,
                UserId = userId
            };

            await _movementClassRepository.AddAsync(movementClass);
            return Ok(movementClassDTO);
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

            var movementClass = await _movementClassRepository.GetByIdAsync(id);
            if (movementClass == null)
            {
                return NotFound();
            }

            if (movementClass.UserId != userId)
            {
                return Unauthorized();
            }

            var movementClassDTO = new MovementClassListDTO
            {
                Description = movementClass.Description
            };

            return Ok(movementClassDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovementClass(int id, MovementClassListDTO movementClassDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var movementClass = await _movementClassRepository.GetByIdAsync(id);
            if (movementClass == null)
            {
                return NotFound();
            }

            if (movementClass.UserId != userId)
            {
                return Unauthorized();
            }

            movementClass.Description = movementClassDTO.Description;
            movementClass.UpdatedAt = DateTime.UtcNow;

            await _movementClassRepository.UpdateAsync(movementClass);

            return Ok(movementClassDTO);
        }

        //finalmente el metodo delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovementClass(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var movementClass = await _movementClassRepository.GetByIdAsync(id);
            if (movementClass == null)
            {
                return NotFound();
            }

            if (movementClass.UserId != userId)
            {
                return Unauthorized();
            }

            await _movementClassRepository.DeleteAsync(id);

            return Ok();
        }

        // ahora quiero uno que reciba un nombre y devuelva true si el user ya tiene un movementClass con ese nombre
        [HttpGet("exists/{description}")]
        public async Task<IActionResult> GetMovementClass(string description ) {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var movementClass = await _movementClassRepository.FindAsync(mc => mc.Description == description && mc.UserId == userId);
            //lo siguiente no funciona, viene vacio y no entra

            if (!movementClass.Any())  
            {
                return NotFound();
            }


            return Ok(true);
        }
    }
}
