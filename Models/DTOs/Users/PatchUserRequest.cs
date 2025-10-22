using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Models.DTOs.Users
{
    public class PatchUserRequest
    {
        [MaxLength(64)]
        public required string Username { get; set; }
        public byte[]? EncryptedVaultBlob { get; set; }
    }
}
