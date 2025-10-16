using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public required Guid UserId { get; set; }
        public Guid? DeviceId { get; set; }

        public required string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Metadata { get; set; }

        // Navigation properties
        public required User User { get; set; }
        public Device? Device { get; set; }
    }
}
