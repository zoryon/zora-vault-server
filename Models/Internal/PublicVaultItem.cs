using System.ComponentModel.DataAnnotations;
using ZoraVault.Models.Entities;

namespace ZoraVault.Models.Internal
{
    public class PublicVaultItem
    {
        public Guid Id { get; set; }

        public VaultItemType Type { get; set; }

        [MaxLength(50000, ErrorMessage = "EncryptedData exceeds the maximum allowed size of 50 KB")]
        public byte[] EncryptedData { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public PublicVaultItem(VaultItem vaultItem)
        {
            Id = vaultItem.Id;
            Type = vaultItem.Type;
            EncryptedData = vaultItem.EncryptedData;
            CreatedAt = vaultItem.CreatedAt;
            UpdatedAt = vaultItem.UpdatedAt;
        }
    }
}
