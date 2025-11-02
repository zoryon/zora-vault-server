using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public enum VaultItemType { 
        Login, 
        Identity,
        Card,
        Note, 
        SSHKey,
    }

    public class VaultItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public required VaultItemType Type { get; set; }

        [MaxLength(50000, ErrorMessage = "EncryptedData exceeds the maximum allowed size of 50 KB")]
        public required byte[] EncryptedData { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
