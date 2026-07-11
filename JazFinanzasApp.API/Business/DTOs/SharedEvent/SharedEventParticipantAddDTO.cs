using System.ComponentModel.DataAnnotations;

namespace JazFinanzasApp.API.Business.DTO.SharedEvent
{
    public class SharedEventParticipantAddDTO
    {
        [Required]
        public int PersonId { get; set; }
    }
}
