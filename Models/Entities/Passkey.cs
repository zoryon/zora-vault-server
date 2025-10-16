using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public class Passkey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public Guid? VaultItemId { get; set; }
        public required byte[] PublicKey { get; set; }
        public required byte[] CredentialId { get; set; }
        public string? DeviceName { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public required User User { get; set; }
        public VaultItem? VaultItem { get; set; }
    }
}
