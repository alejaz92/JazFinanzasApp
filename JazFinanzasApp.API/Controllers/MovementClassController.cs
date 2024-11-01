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
        private readonly IMovementClassRepository _movementClassRepository;
        public MovementClassController(IMovementClassRepository movementClassRepository)
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


            movementClasses = movementClasses.OrderBy(mc => mc.Description);

            // convert to DTO

            var movementClassesDTO = movementClasses.Select(mc => new MovementClassDTO
            {
                Id = mc.Id,
                Description = mc.Description,
                IncExp = mc.IncExp
            }).ToList();
            return Ok(movementClassesDTO);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMovementClass(Models.DTO.movementClasses.MovementClassDTO movementClassDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var checkExists = await _movementClassRepository.FindAsync(mc => mc.Description == movementClassDTO.Description && 
                mc.UserId == userId);
            //lo siguiente no funciona, viene vacio y no entra

            if (checkExists.Any())
            {
                return BadRequest("Movement Class already exists");
            }

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

            var movementClassDTO = new Models.DTO.movementClasses.MovementClassDTO
            {
                Description = movementClass.Description,
                IncExp = movementClass.IncExp
            };

            return Ok(movementClassDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovementClass(int id, Models.DTO.movementClasses.MovementClassDTO movementClassDTO)
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



        //cuando tenga armado movimientos, hacer la funcion que chequee si una clase se usa en la tabla de movimientos

        
    }
}
