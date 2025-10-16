using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public class Device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public required Guid UserId { get; set; }

        // Deterministic unique fingerprint (SHA256 of public key)
        [MaxLength(64)] // SHA256 hash (in hex)
        public required string Fingerprint { get; set; }
        public required string DeviceName { get; set; }
        public required byte[] PublicKey { get; set; }
        public bool IsTrusted { get; set; } = false;
        public string? TempChallenge { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; }

        // Navigation properties
        public required User User { get; set; }
    }
}
