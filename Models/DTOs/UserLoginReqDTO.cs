namespace ZoraVault.Models.DTOs
{
    public class UserLoginReqDTO
    {
        public required string UsernameOrEmail { get; set; }
        public required string PasswordHash { get; set; } // Client-side hashed password
        public required string PublicKey { get; set; } // Client-side generated public key for one specific device
    }
}
