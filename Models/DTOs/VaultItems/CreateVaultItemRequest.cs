using ZoraVault.Models.Entities;

namespace ZoraVault.Models.DTOs.VaultItems
{
    public class CreateVaultItemRequest
    {
        public required VaultItemType Type { get; set; }
        public required byte[] EncryptedData { get; set; }
    }
}
