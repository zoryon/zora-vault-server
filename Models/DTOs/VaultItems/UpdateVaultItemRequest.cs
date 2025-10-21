using System.ComponentModel.DataAnnotations;
using ZoraVault.Models.Entities;

namespace ZoraVault.Models.DTOs.VaultItems
{
    public class UpdateVaultItemRequest
    {
        public required VaultItemType Type { get; set; }

        [MaxLength(50000, ErrorMessage = "EncryptedData exceeds the maximum allowed size of 50 KB.")]
        public required byte[] EncryptedData { get; set; }
    }
}
