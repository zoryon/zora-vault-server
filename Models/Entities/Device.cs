using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public class Device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [MaxLength(64)]
        public required byte[] Fingerprint { get; set; } // SHA256(publicKey)

        [MaxLength(8192)]
        public required byte[] PublicKey { get; set; }

        [MaxLength(8192)]
        public byte[]? TempChallenge { get; set; }

        public DateTime? TempChallengeIssuedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; }

        // Navigation properties
        public ICollection<UserDevice> UserDevices { get; set; } = [];
        public ICollection<Session> Sessions { get; set; } = [];
    }
}
