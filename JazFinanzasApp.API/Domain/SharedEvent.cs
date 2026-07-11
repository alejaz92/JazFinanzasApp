using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JazFinanzasApp.API.Domain
{
    public class SharedEvent : BaseEntity
    {
        [Required]
        public string Name { get; set; }

        public string? Notes { get; set; }

        [Required]
        public bool IsClosed { get; set; } = false;

        [Required]
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<SharedEventParticipant> Participants { get; set; }
        public ICollection<SharedEventMovement> Movements { get; set; }
        public ICollection<SharedEventPayment> Payments { get; set; }
    }
}
