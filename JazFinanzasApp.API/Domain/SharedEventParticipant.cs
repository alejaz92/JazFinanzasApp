using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEventParticipant : BaseEntity
    {
        [Required]
        [ForeignKey("SharedEventId")]
        public int SharedEventId { get; set; }
        public SharedEvent SharedEvent { get; set; }

        [Required]
        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }
    }
}
