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
        public required string PublicKey { get; set; }
        public string? TempChallenge { get; set; }
        public DateTime? TempChallengeIssuedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
