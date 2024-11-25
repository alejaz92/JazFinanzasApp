using JazFinanzasApp.API.Interfaces;
using JazFinanzasApp.API.Models.Domain;
using JazFinanzasApp.API.Models.DTO.transactionClass;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JazFinanzasApp.API.Controllers
{


    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionClassController : ControllerBase
    {
        private readonly ITransactionClassRepository _transactionClassRepository;
        public TransactionClassController(ITransactionClassRepository transactionClassRepository)
        {
            _transactionClassRepository = transactionClassRepository;
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

            var transactionClasses = await _transactionClassRepository.GetByUserIdAsync(userId);


            transactionClasses = transactionClasses.OrderBy(mc => mc.Description);

            // convert to DTO

            var transactionClassesDTO = transactionClasses.Select(mc => new TransactionClassDTO
            {
                Id = mc.Id,
                Description = mc.Description,
                IncExp = mc.IncExp
            }).ToList();
            return Ok(transactionClassesDTO);
        }


        [HttpPost]
        public async Task<IActionResult> CreateTransactionClass(Models.DTO.transactionClass.TransactionClassDTO transactionClassDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var checkExists = await _transactionClassRepository.FindAsync(mc => mc.Description == transactionClassDTO.Description && 
                mc.UserId == userId);
            //lo siguiente no funciona, viene vacio y no entra

            if (checkExists.Any())
            {
                return BadRequest("Movement Class already exists");
            }

            var transactionClass = new TransactionClass
            {
                Description = transactionClassDTO.Description,
                IncExp = transactionClassDTO.IncExp,
                UserId = userId
            };

            await _transactionClassRepository.AddAsync(transactionClass);
            return Ok(transactionClassDTO);
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

            var transactionClass = await _transactionClassRepository.GetByIdAsync(id);
            if (transactionClass == null)
            {
                return NotFound();
            }

            if (transactionClass.UserId != userId)
            {
                return Unauthorized();
            }

            var transactionClassDTO = new Models.DTO.transactionClass.TransactionClassDTO
            {
                Description = transactionClass.Description,
                IncExp = transactionClass.IncExp
            };

            return Ok(transactionClassDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransactionClass(int id, Models.DTO.transactionClass.TransactionClassDTO transactionClassDTO)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var transactionClass = await _transactionClassRepository.GetByIdAsync(id);
            if (transactionClass == null)
            {
                return NotFound();
            }

            if (transactionClass.UserId != userId)
            {
                return Unauthorized();
            }

            transactionClass.Description = transactionClassDTO.Description;
            transactionClass.UpdatedAt = DateTime.UtcNow;

            await _transactionClassRepository.UpdateAsync(transactionClass);

            return Ok(transactionClassDTO);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransactionClass(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var transactionClass = await _transactionClassRepository.GetByIdAsync(id);
            if (transactionClass == null)
            {
                return NotFound();
            }

            if (transactionClass.UserId != userId)
            {
                return Unauthorized();
            }

            await _transactionClassRepository.DeleteAsync(id);

            return Ok();
        }



        //cuando tenga armado movimientos, hacer la funcion que chequee si una clase se usa en la tabla de movimientos

        
    }
}
