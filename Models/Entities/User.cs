using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZoraVault.Models.Internal;

namespace ZoraVault.Models.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [MaxLength(64)]
        public required string Username { get; set; }

        [MaxLength(254)]
        public required string Email { get; set; }

        [MaxLength(4096)]
        public required string ServerPasswordHash { get; set; }

        [MaxLength(256)]
        public required string ServerSalt { get; set; }
        public required KdfParams KdfParams { get; set; }
        public byte[]? EncryptedVaultBlob { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<VaultItem> VaultItems { get; set; } = [];
        public ICollection<Device> Devices { get; set; } = [];
        public ICollection<AuditLog> AuditLogs { get; set; } = [];
    }
}
