using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoraVault.Models.Entities
{
    public enum VaultItemType { Password, Note, Attachment }

    public class VaultItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public required VaultItemType Type { get; set; }

        public required byte[] EncryptedData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public required User User { get; set; }
    }
}
