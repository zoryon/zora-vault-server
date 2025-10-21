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
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string ServerPasswordHash { get; set; }
        public required string ServerSalt { get; set; }
        public required KdfParams KdfParams { get; set; }
        public string? EncryptedVaultBlob { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<VaultItem> VaultItems { get; set; } = [];
        public ICollection<Passkey> Passkeys { get; set; } = [];
        public ICollection<Device> Devices { get; set; } = [];
        public ICollection<AuditLog> AuditLogs { get; set; } = [];
    }
}
